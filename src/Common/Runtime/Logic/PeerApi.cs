using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Shared.Log;
using Shared.Tp;
using Shared.Web;

namespace Common.Logic
{
    [Serializable]
    public class ConnectState
    {
        public string PeerId;
        public ConnectState(string peerId)
        {
            PeerId = peerId;
        }
        public override string ToString() => $"ConnectState({PeerId})"; //diagnostics only

        public byte[] Serialize()
        {
            var str = WebSerializer.SerializeObject(this);
            return Encoding.UTF8.GetBytes(str);
        }

        public static ConnectState Deserialize(Span<byte> bytes)
        {
            var str = Encoding.UTF8.GetString(bytes);
            return WebSerializer.DeserializeObject<ConnectState>(str);
        }        
    }

    public class HandshakeOptions
    {
        public int TimeoutMs = 5000;
        public int SynRetryMs = 500; //TODO: incremental retry support
    }

    /// <summary>
    /// TODO: custom initial state (not only peer id is required for logic)
    /// </summary>
    public class PeerApi : ExtApi<PeerLink>
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly ConnectState? _connectState;

        public HandshakeOptions HandshakeOptions { get; } = new();

        public PeerApi(ITpApi innerApi, ConnectState? connectState, ILoggerFactory loggerFactory) 
            : base(innerApi)
        {
            _connectState = connectState;
            _loggerFactory = loggerFactory;
            var logger = loggerFactory.CreateLogger<PeerApi>();
            logger.Info($"connectState: {_connectState}");
        }

        protected override PeerLink CreateClientLink(ITpReceiver receiver)
        {
            if (_connectState == null)
                throw new InvalidOperationException("cannot connect without connection state specified");
            return new(this, receiver, _connectState!, _loggerFactory);
        }

        protected override PeerLink CreateServerLink(ITpLink innerLink) =>
            new(this, innerLink, ConnectState.Deserialize, _loggerFactory);

        /// <summary>
        /// Connect is overriden to delay link return until handshake isn't complete 
        /// </summary>
        public override async ValueTask<ITpLink> Connect(ITpReceiver receiver, CancellationToken cancellationToken)
        {
            var link = (PeerLink)await base.Connect(receiver, cancellationToken);
            await link.Handshake(cancellationToken);
            return link;
        }

        /// <summary>
        /// Connected is overriden to postpone listener notification until handshake isn't complete 
        /// </summary>
        public override ITpReceiver Connected(ITpLink link)
        {
            var extLink = CreateServerLink(link);
            return extLink;
        }
    }
}