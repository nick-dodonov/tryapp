using System;
using System.Buffers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Shared.Log;

namespace Shared.Tp.Ext.Hand
{
    public interface IHandLocalStateProvider
    {
        int Serialize(IBufferWriter<byte> writer);
    }

    public interface IHandRemoteStateProvider<out TState>
    {
        TState Deserialize(ReadOnlySpan<byte> span);
    }

    public class HandshakeOptions
    {
        public readonly int TimeoutMs = 5000;
        public readonly int SynRetryMs = 500; //TODO: incremental retry support
    }

    public class HandApi<TRemoteState> : ExtApi<HandLink<TRemoteState>>
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly IHandLocalStateProvider _localStateProvider;
        private readonly IHandRemoteStateProvider<TRemoteState> _remoteStateProvider;

        private readonly HandLink<TRemoteState>.LinkIdProvider _linkIdProvider;
        
        public HandshakeOptions HandshakeOptions { get; } = new();

        public HandApi(
            ITpApi innerApi, 
            IHandLocalStateProvider localStateProvider, 
            IHandRemoteStateProvider<TRemoteState> remoteStateProvider, 
            HandLink<TRemoteState>.LinkIdProvider linkIdProvider,
            ILoggerFactory loggerFactory) 
            : base(innerApi)
        {
            _localStateProvider = localStateProvider;
            _remoteStateProvider = remoteStateProvider;
            _linkIdProvider = linkIdProvider;
            _loggerFactory = loggerFactory;
            Slog.Info($"state providers: local={localStateProvider} remote={remoteStateProvider}");
        }

        protected override HandLink<TRemoteState> CreateClientLink(ITpReceiver receiver) 
            => new(this, receiver, _localStateProvider, _remoteStateProvider, _linkIdProvider, _loggerFactory);

        protected override HandLink<TRemoteState> CreateServerLink(ITpLink innerLink) =>
            new(this, innerLink, _localStateProvider, _remoteStateProvider,_linkIdProvider, _loggerFactory);

        /// <summary>
        /// Connect is overriden to delay link return until handshake isn't complete 
        /// </summary>
        public override async ValueTask<ITpLink> Connect(ITpReceiver receiver, CancellationToken cancellationToken)
        {
            var link = (HandLink<TRemoteState>)await base.Connect(receiver, cancellationToken);
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