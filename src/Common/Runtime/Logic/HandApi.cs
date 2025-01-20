using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Shared.Log;
using Shared.Tp;

namespace Common.Logic
{
    public interface IHandConnectState
    {
        public string PeerId { get; }
    }

    public interface IHandStateProvider
    {
        IHandConnectState ProvideConnectState();
        byte[] Serialize(IHandConnectState connectState);
        IHandConnectState Deserialize(Span<byte> asSpan);
    }
    
    public class HandshakeOptions
    {
        public int TimeoutMs = 5000;
        public int SynRetryMs = 500; //TODO: incremental retry support
    }

    /// <summary>
    /// TODO: custom initial state (not only peer id is required for logic)
    /// </summary>
    public class HandApi : ExtApi<HandLink>
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly IHandStateProvider _stateProvider;

        public HandshakeOptions HandshakeOptions { get; } = new();

        public HandApi(ITpApi innerApi, IHandStateProvider stateProvider, ILoggerFactory loggerFactory) 
            : base(innerApi)
        {
            _stateProvider = stateProvider;
            _loggerFactory = loggerFactory;
            var logger = loggerFactory.CreateLogger<HandApi>();
            logger.Info($"state provider: {stateProvider}");
        }

        protected override HandLink CreateClientLink(ITpReceiver receiver) 
            => new(this, receiver, _stateProvider, _loggerFactory);

        protected override HandLink CreateServerLink(ITpLink innerLink) =>
            new(this, innerLink, _stateProvider, _loggerFactory);

        /// <summary>
        /// Connect is overriden to delay link return until handshake isn't complete 
        /// </summary>
        public override async ValueTask<ITpLink> Connect(ITpReceiver receiver, CancellationToken cancellationToken)
        {
            var link = (HandLink)await base.Connect(receiver, cancellationToken);
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