using System;
using System.Buffers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Shared.Log;

namespace Shared.Tp.Ext.Hand
{
    public interface IHandStateProvider<TState>
    {
        TState ProvideState();
        string GetLinkId(TState state);

        int Serialize(IBufferWriter<byte> writer, TState state);
        TState Deserialize(ReadOnlySpan<byte> span);
    }

    public class HandshakeOptions
    {
        public int TimeoutMs = 5000;
        public int SynRetryMs = 500; //TODO: incremental retry support
    }

    public class HandApi<TState> : ExtApi<HandLink<TState>> where TState : class
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly IHandStateProvider<TState> _stateProvider;

        public HandshakeOptions HandshakeOptions { get; } = new();

        public HandApi(ITpApi innerApi, IHandStateProvider<TState> stateProvider, ILoggerFactory loggerFactory) 
            : base(innerApi)
        {
            _stateProvider = stateProvider;
            _loggerFactory = loggerFactory;
            var logger = loggerFactory.CreateLogger<HandApi<TState>>();
            logger.Info($"state provider: {stateProvider}");
        }

        protected override HandLink<TState> CreateClientLink(ITpReceiver receiver) 
            => new(this, receiver, _stateProvider, _loggerFactory);

        protected override HandLink<TState> CreateServerLink(ITpLink innerLink) =>
            new(this, innerLink, _stateProvider, _loggerFactory);

        /// <summary>
        /// Connect is overriden to delay link return until handshake isn't complete 
        /// </summary>
        public override async ValueTask<ITpLink> Connect(ITpReceiver receiver, CancellationToken cancellationToken)
        {
            var link = (HandLink<TState>)await base.Connect(receiver, cancellationToken);
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