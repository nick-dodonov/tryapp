using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Shared.Log;
using Shared.Log.Asp;
using Shared.Tp.St;
using Shared.Tp.Util;

namespace Shared.Tp.Ext.Hand
{
    /// <summary>
    /// Wrapper link for establishing connection with initial reliable state:
    ///   client->server initial state, simple 3-way handshake implemented now:
    ///     * client send/resend syn state until ack is received
    ///     * server send ack state immediately and resend it with messages until syn-ack flag isn't received
    ///     * client send syn-ack every time ack is received 
    ///
    /// TODO: reconnect support (another underlying wrapper via provider's link id)
    /// 
    /// </summary>
    public class HandLink<TRemoteState> : ExtLink
    {
        private readonly HandApi<TRemoteState> _api = null!;
        private readonly ILoggerFactory _loggerFactory = null!;

        public delegate string LinkIdProvider(HandLink<TRemoteState> link);
        private readonly LinkIdProvider _linkIdProvider = null!;

        private ILogger _logger = null!;

        private readonly IOwnWriter _localStateWriter = null!;
        private readonly IReader<TRemoteState> _remoteStateReader = null!;
        private bool _localStateDelivered; //TODO: interlocked 
        private TRemoteState? _remoteState;

        public TRemoteState? RemoteState => _remoteState;

        /// <summary>
        /// Flags required for initial reliable state handshaking
        /// </summary>
        [Flags]
        private enum Flags : byte
        {
            Zero = 0, // only original payload message
            Syn = 1 << 1, // client->server connect message: body is a reliable initial client's syn state 
            Ack = 1 << 4 // server->client answer message: means initial state is received  
        }

        private HandSynState? _synState; //null means ack received or doesn't required

        public HandLink() { } //empty constructor only for generic usage

        // client side
        public HandLink(HandApi<TRemoteState> api, ITpReceiver receiver, 
            IOwnWriter localStateWriter, 
            IReader<TRemoteState> remoteStateReader,
            LinkIdProvider linkIdProvider,
            ILoggerFactory loggerFactory)
            : base(receiver)
        {
            _api = api;
            _localStateWriter = localStateWriter;
            _remoteStateReader = remoteStateReader;
            _linkIdProvider = linkIdProvider;
            _loggerFactory = loggerFactory;
            InitLogger();
        }

        // server side
        public HandLink(HandApi<TRemoteState> api, ITpLink innerLink, 
            IOwnWriter localStateWriter, 
            IReader<TRemoteState> remoteStateReader, 
            LinkIdProvider linkIdProvider,
            ILoggerFactory loggerFactory)
            : base(innerLink)
        {
            _api = api;
            _localStateWriter = localStateWriter;
            _remoteStateReader = remoteStateReader;
            _linkIdProvider = linkIdProvider;
            _loggerFactory = loggerFactory;
            InitLogger();
        }

        private void InitLogger()
        {
            _logger = new IdLogger(
                _loggerFactory.CreateLogger(nameof(HandLink<TRemoteState>)),
                _linkIdProvider(this));
        }
        
        protected override void Close(string reason)
        {
            _logger.Info(reason);
            base.Close(reason);
        }

        public async Task Handshake(CancellationToken cancellationToken)
        {
            try
            {
                _synState = new(_api.HandshakeOptions);
                do
                {
                    _logger.Info(_synState.Attempts == 0 
                        ? $"send syn and wait ack: {_localStateWriter}"
                        : $"resend syn and wait ack ({_synState.Attempts} attempt): {_localStateWriter}");
                    base.Send((writer, @this) =>
                    {
                        writer.Write((byte)Flags.Syn);
                        @this._localStateWriter.Serialize(writer);
                    }, this);
                } while (await _synState.AwaitResend(cancellationToken));

                _localStateDelivered = true;
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
        }

        private int _resendSynAck = (byte)Flags.Zero;
        public override void Send<T>(TpWriteCb<T> writeCb, in T state)
        {
            if (_localStateDelivered)
            {
                if (_resendSynAck == 0)
                {
                    // client/server sides: the handshake is completed
                    base.Send(static (writer, s) =>
                    {
                        writer.Write((byte)Flags.Zero);
                        s.writeCb(writer, s.state);
                    }, (writeCb, state));
                }
                else
                {
                    // client side: resend syn-ack as ack was received
                    base.Send(static (writer, s) =>
                    {
                        var @this = s.Item3;
                        if (Interlocked.Exchange(ref @this._resendSynAck, 0) != 0) // handle concurrent send happened
                        {
                            @this._logger.Info($"resend syn-ack with message");
                            writer.Write((byte)(Flags.Syn | Flags.Ack));
                        }
                        else
                            writer.Write((byte)Flags.Zero);
                        s.writeCb(writer, s.state);
                    }, (writeCb, state, this));
                }
            }
            else
            {
                // server side: attach ack state until syn-ack flag isn't received
                base.Send(static (writer, s) =>
                {
                    var @this = s.Item3;
                    if (!@this._localStateDelivered) // handle concurrent syn-ack received //TODO: CAS
                    {
                        var localStateWriter = @this._localStateWriter;
                        @this._logger.Info($"resend ack with message: {localStateWriter}");
                        writer.Write((byte)Flags.Ack);
                        writer.PrependSizeWrite(localStateWriter);
                    }
                    else
                    {
                        writer.Write((byte)Flags.Zero);
                        s.writeCb(writer, s.state);
                    }
                }, (writeCb, state, this));
            }
        }

        public override void Received(ITpLink link, ReadOnlySpan<byte> span)
        {
            var flags = (Flags)span[0];
            span = span[1..];

            // server side
            if ((flags & Flags.Syn) != 0)
            {
                if ((flags & Flags.Ack) != 0)
                {
                    if (!_localStateDelivered)
                    {
                        _logger.Info("syn-ack: stop ack with messages");
                        _localStateDelivered = true;
                    }
                    else
                    {
                        _logger.Info("syn-ack duplicate");
                    }
                }
                else
                {
                    if (_remoteState != null)
                    {
                        _logger.Info("syn duplicate");
                        return;
                    }

                    _remoteState = _remoteStateReader.Deserialize(span);
                    _logger.Info($"syn state: {_remoteState}");
                    InitLogger(); // reinitialize logger to include remote state now (peer id to simplify diagnostics)

                    // notify listener connection is established after a handshake
                    if (_api.CallConnected(this))
                    {
                        _logger.Info($"send ack: {_localStateWriter}");
                        base.Send(static (writer, @this) =>
                        {
                            writer.Write((byte)Flags.Ack);
                            writer.PrependSizeWrite(@this._localStateWriter);
                        }, this);
                    }
                    else
                        _logger.Info("disconnect on listen (rejected)");

                    return;
                }
            }
            else if ((flags & Flags.Ack) != 0) // client side
            {
                var ackStateSize = SpanReader.Read<short>(span[..sizeof(short)]);
                if (_remoteState == null)
                {
                    _remoteState = _remoteStateReader.Deserialize(span.Slice(sizeof(short), ackStateSize));
                    _logger.Info($"ack state: {_remoteState}");

                    if (_synState != null)
                        Interlocked.Exchange(ref _synState, null)?.AckReceived();

                    _logger.Info($"send syn-ack");
                    base.Send(static (writer, _) =>
                    {
                        writer.Write((byte)(Flags.Syn | Flags.Ack));
                    }, 0);
                }
                else
                {
                    _logger.Info("ack duplicate");
                }

                span = span[(sizeof(short) + ackStateSize)..];
            }

            if (span.Length <= 0)
                return; // ignore an empty message (initial ack or syn-ack)

            if (_synState != null)
            {
                _logger.Info($"skip without handshake: {span.Length} bytes");
                return; // ignore messages while handshake in progress
            }

            base.Received(link, span);
        }
    }
}