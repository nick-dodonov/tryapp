using System;
using System.Buffers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Shared.Log;
using Shared.Log.Asp;

namespace Shared.Tp.Hand
{
    /// <summary>
    /// Wrapper link for establishing connection with initial reliable state:
    ///   client->server initial state, simple 2-way handshake implemented now:
    ///     * client send/resend syn state until ack is received
    ///     * server answers ack immediately and includes ack with user data 
    ///
    /// TODO: speedup to gc-free on one buffer after changing link/receiver API 
    /// TODO: reconnect support (possibly another wrapper)
    /// </summary>
    public class HandLink : ExtLink
    {
        private readonly HandApi _api = null!;
        private readonly ILoggerFactory _loggerFactory = null!;
        private ILogger _logger = null!;

        private readonly IHandStateProvider _stateProvider = null!;
        private IHandConnectState? _connectState;

        /// <summary>
        /// Flags required for initial reliable state handshaking
        /// </summary>
        [Flags]
        private enum Flags : byte
        {
            Syn = 1 << 1, // client->server connection message: body is reliable initial state 
            Ack = 1 << 4 // server->client message flag: means initial state is received  
        }

        private HandSynState? _synState; //null means ack received or doesn't required

        public HandLink() { } //empty constructor only for generic usage

        public HandLink(HandApi api, ITpReceiver receiver, 
            IHandStateProvider stateProvider, ILoggerFactory loggerFactory)
            : base(receiver)
        {
            _api = api;
            _stateProvider = stateProvider;
            _connectState = _stateProvider.ProvideConnectState();
            _loggerFactory = loggerFactory;
            _logger = new IdLogger(loggerFactory.CreateLogger<HandLink>(), _connectState.LinkId);
        }

        public HandLink(HandApi api, ITpLink innerLink, 
            IHandStateProvider stateProvider, ILoggerFactory loggerFactory)
            : base(innerLink)
        {
            _api = api;
            _stateProvider = stateProvider;
            _loggerFactory = loggerFactory;
            _logger = new IdLogger(loggerFactory.CreateLogger<HandLink>(), GetRemotePeerId());
        }

        protected override void Close(string reason)
        {
            _logger.Info(reason);
            base.Close(reason);
        }

        public async Task Handshake(CancellationToken cancellationToken)
        {
            var connectState = _connectState!;
            _logger.Info($"send syn and wait ack: {connectState}");

            var stateBytes = _stateProvider.Serialize(connectState);
            var synBytes = ArrayPool<byte>.Shared.Rent(stateBytes.Length + 1);
            try
            {
                synBytes[0] = (byte)Flags.Syn;
                stateBytes.CopyTo(synBytes.AsSpan(1));
                InnerLink.Send(synBytes.AsSpan(0, stateBytes.Length + 1));
                // InnerLink.Send<ReadOnlySpan<byte>>(static (writer, span) =>
                // {
                //     writer.Write(span);
                // }, sendSpan);

                _synState = new(_api.HandshakeOptions);
                while (await _synState.AwaitResend(cancellationToken))
                {
                    _logger.Info($"resend syn waiting ack ({_synState.Attempts} attempt): {connectState}");
                    InnerLink.Send(synBytes.AsSpan(0, stateBytes.Length + 1));
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
            $"{_connectState?.LinkId}/{InnerLink.GetRemotePeerId()}"; //TODO: speedup without string interpolation

        public override void Send(ReadOnlySpan<byte> span)
        {
            var sendBytes = ArrayPool<byte>.Shared.Rent(span.Length + 1);
            try
            {
                sendBytes[0] = (byte)Flags.Ack;
                span.CopyTo(sendBytes.AsSpan(1));
                base.Send(sendBytes.AsSpan(0, span.Length + 1));
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(sendBytes);
            }
        }

        public override void Received(ITpLink link, ReadOnlySpan<byte> span)
        {
            var flags = (Flags)span[0];

            span = span[1..];
            if ((flags & Flags.Syn) != 0)
            {
                if (_connectState != null)
                    return; // duplicate syn received: already connected

                _connectState = _stateProvider.Deserialize(span);
                _logger.Info($"received connection state: {_connectState}");
                _logger = new IdLogger(_loggerFactory.CreateLogger<HandLink>(), GetRemotePeerId());

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

            if (span.Length <= 0)
                return; // ignore empty message (initial ack)

            if (_synState != null)
            {
                _logger.Info($"skip without handshake: {span.Length} bytes");
                return; // ignore messages while handshake in progress
            }

            base.Received(link, span);
        }
    }
}