using System;
using System.Buffers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Shared.Log;
using Shared.Log.Asp;
using Shared.Tp;

namespace Common.Logic
{
    /// <summary>
    /// Wrapper link for establishing connection with initial reliable state:
    ///   client->server initial state, simple 2-way handshake implemented now:
    ///     * client send/resend syn state until ack is received
    ///     * server answers ack immediately and includes ack with user data 
    ///
    /// TODO: rfx to generic with any sync state (not only peer Id)
    /// TODO: speedup to gc-free on one buffer after changing link/receiver API 
    /// TODO: reconnect support (possibly another wrapper)
    /// </summary>
    public class PeerLink : ExtLink
    {
        private readonly PeerApi _api = null!;
        private readonly ILoggerFactory _loggerFactory = null!;
        private ILogger _logger = null!;

        private string? _peerId;

        /// <summary>
        /// Flags required for initial reliable state handshaking
        /// </summary>
        [Flags]
        private enum Flags : byte
        {
            Syn = 1 << 1, // client->server connection message: body is reliable initial state (peer id) 
            Ack = 1 << 4 // server->client message flag: means initial state is received  
        }

        private PeerSynState? _synState; //null means ack received or doesn't required

        public PeerLink()
        {
        } //empty constructor only for generic usage

        public PeerLink(PeerApi api, ITpReceiver receiver, string peerId, ILoggerFactory loggerFactory)
            : base(receiver)
        {
            _api = api;
            _loggerFactory = loggerFactory;
            _logger = new IdLogger(loggerFactory.CreateLogger<PeerLink>(), peerId);
            _peerId = peerId;
        }

        public PeerLink(PeerApi api, ITpLink innerLink, ILoggerFactory loggerFactory)
            : base(innerLink)
        {
            _api = api;
            _loggerFactory = loggerFactory;
            _logger = new IdLogger(loggerFactory.CreateLogger<PeerLink>(), GetRemotePeerId());
        }

        public override void Close(string reason)
        {
            _logger.Info(reason);
            base.Close(reason);
        }

        public async Task Handshake(CancellationToken cancellationToken)
        {
            var peerId = _peerId!;
            _logger.Info($"send syn and wait ack: {peerId}");

            var charCount = peerId.Length;
            var maxByteLength = Encoding.UTF8.GetMaxByteCount(charCount);
            maxByteLength++; //flags byte

            var synBytes = ArrayPool<byte>.Shared.Rent(maxByteLength);
            try
            {
                synBytes[0] = (byte)Flags.Syn;
                var encodedCount = Encoding.UTF8.GetBytes(peerId.AsSpan(), synBytes.AsSpan(1));

                var sendBytes = synBytes.AsSpan(0, 1 + encodedCount).ToArray();
                InnerLink.Send(sendBytes);

                _synState = new(_api.HandshakeOptions);
                while (await _synState.AwaitResend(cancellationToken))
                {
                    _logger.Info($"resend syn waiting ack ({_synState.Attempts} attempt): {peerId}");
                    InnerLink.Send(sendBytes);
                }

                _logger.Info("connected");
            }
            catch (Exception e)
            {
                if (e is TimeoutException or TaskCanceledException) 
                    _logger.Warn(e.Message);
                else 
                    _logger.Error($"fail: {e}");
                Close("handshake failed");
                throw;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(synBytes);
            }
        }

        public sealed override string GetRemotePeerId() =>
            $"{_peerId}/{InnerLink.GetRemotePeerId()}"; //TODO: speedup without string interpolation

        public override void Send(byte[] bytes)
        {
            var sendBytes = ArrayPool<byte>.Shared.Rent(bytes.Length + 1);
            try
            {
                sendBytes[0] = (byte)Flags.Ack;
                bytes.AsSpan().CopyTo(sendBytes.AsSpan(1));
                base.Send(sendBytes.AsSpan(0, bytes.Length + 1).ToArray());
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(sendBytes);
            }
        }

        public override void Received(ITpLink link, byte[]? bytes)
        {
            if (bytes != null)
            {
                var flags = (Flags)bytes[0];

                if ((flags & Flags.Syn) != 0)
                {
                    if (_peerId != null)
                        return; // duplicate syn received: already connected

                    _peerId = Encoding.UTF8.GetString(bytes.AsSpan(1).ToArray());
                    _logger.Info($"received peer id: {_peerId}");
                    _logger = new IdLogger(_loggerFactory.CreateLogger<PeerLink>(), GetRemotePeerId());

                    // notify listener connection is established after handshake
                    if (_api.CallConnected(this))
                    {
                        _logger.Info("sending empty ack");
                        Send(Array.Empty<byte>());
                    }
                    else
                        _logger.Info("disconnected on listen");

                    return;
                }

                if ((flags & Flags.Ack) != 0 && _synState != null)
                {
                    _logger.Info("ack received");
                    _synState.AckReceived();
                    _synState = null;
                }

                bytes = bytes.AsSpan(1).ToArray();
                if (bytes.Length <= 0)
                    return; // ignore empty message (usually initial ack)

                if (_synState != null)
                {
                    _logger.Info($"skip without handshake: {bytes.Length} bytes");
                    return; // ignore messages while handshake in progress
                }
            }

            base.Received(link, bytes);
        }
    }
}