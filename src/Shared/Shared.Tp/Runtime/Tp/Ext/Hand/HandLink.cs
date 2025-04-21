using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Shared.Log;
using Shared.Log.Asp;
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
    /// TODO: speedup to gc-free on one buffer after changing link/receiver API 
    /// TODO: reconnect support (possibly another wrapper)
    /// </summary>
    public partial class HandLink : ExtLink
    {
        private readonly HandApi _api = null!;
        private readonly ILoggerFactory _loggerFactory = null!;
        private ILogger _logger = null!;

        private readonly IHandStateProvider _stateProvider = null!;
        private IHandConnectState? _localState;
        private IHandConnectState? _remoteState;

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
        public HandLink(HandApi api, ITpReceiver receiver, 
            IHandStateProvider stateProvider, ILoggerFactory loggerFactory)
            : base(receiver)
        {
            _api = api;
            _stateProvider = stateProvider;
            _localState = _stateProvider.ProvideConnectState();
            _loggerFactory = loggerFactory;
            _logger = new IdLogger(loggerFactory.CreateLogger<HandLink>(), _localState.LinkId);
        }

        // server side
        public HandLink(HandApi api, ITpLink innerLink, 
            IHandStateProvider stateProvider, ILoggerFactory loggerFactory)
            : base(innerLink)
        {
            _api = api;
            _stateProvider = stateProvider;
            _localState = _stateProvider.ProvideConnectState();
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
            var synState = _localState!;
            try
            {
                _synState = new(_api.HandshakeOptions);
                do
                {
                    _logger.Info(_synState.Attempts == 0 
                        ? $"send syn and wait ack: {synState}"
                        : $"resend syn and wait ack ({_synState.Attempts} attempt): {synState}");
                    base.Send((writer, @this) =>
                    {
                        writer.Write((byte)Flags.Syn);
                        @this._stateProvider.Serialize(writer, @this._localState!);
                    }, this);
                } while (await _synState.AwaitResend(cancellationToken));

                _localState = null;
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

        public sealed override string GetRemotePeerId() =>
            $"{_remoteState?.LinkId}/{InnerLink.GetRemotePeerId()}"; //TODO: speedup without string interpolation

        private int _resendSynAck = (byte)Flags.Zero;
        public override void Send<T>(TpWriteCb<T> writeCb, in T state)
        {
            if (_localState == null)
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
                    var localState = @this._localState;
                    if (localState != null) // handle concurrent syn-ack received //TODO: CAS
                    {
                        @this._logger.Info($"resend ack with message: {localState}");
                        writer.Write((byte)Flags.Ack);
                        var ackStateSize = @this._stateProvider.Serialize(writer, localState);
                        s.writeCb(writer, s.state);
                        writer.Write((short)ackStateSize);
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
                    if (_localState != null)
                    {
                        _logger.Info("syn-ack: stop ack with messages");
                        _localState = null;
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

                    _remoteState = _stateProvider.Deserialize(span);
                    _logger.Info($"syn state: {_remoteState}");
                    _logger = new IdLogger(_loggerFactory.CreateLogger<HandLink>(), GetRemotePeerId());

                    // notify listener connection is established after a handshake
                    if (_api.CallConnected(this))
                    {
                        _logger.Info($"send ack: {_localState}");
                        base.Send(static (writer, @this) =>
                        {
                            writer.Write((byte)Flags.Ack);
                            var ackStateSize = @this._stateProvider.Serialize(writer, @this._localState!);
                            writer.Write((short)ackStateSize);
                        }, this);
                    }
                    else
                        _logger.Info("disconnect on listen (rejected)");

                    return;
                }
            }
            else if ((flags & Flags.Ack) != 0) // client side
            {
                var ackStateSize = SpanReader.Read<short>(span[^sizeof(short)..]);
                if (_remoteState == null)
                {
                    _remoteState = _stateProvider.Deserialize(span[..ackStateSize]);
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

                span = span[ackStateSize..^sizeof(short)];
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