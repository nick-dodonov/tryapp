using System;
using System.Buffers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Shared.Log;

namespace Shared.Tp.Ext.Hand
{
    public interface IOwnStateWriter
    {
        int Serialize(IBufferWriter<byte> writer);
    }

    public interface IStateReader<out TState>
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
        private readonly IOwnStateWriter _localStateWriter;
        private readonly IStateReader<TRemoteState> _remoteStateReader;

        private readonly HandLink<TRemoteState>.LinkIdProvider _linkIdProvider;
        
        public HandshakeOptions HandshakeOptions { get; } = new();

        public HandApi(
            ITpApi innerApi, 
            IOwnStateWriter localStateWriter, 
            IStateReader<TRemoteState> remoteStateReader, 
            HandLink<TRemoteState>.LinkIdProvider linkIdProvider,
            ILoggerFactory loggerFactory) 
            : base(innerApi)
        {
            _localStateWriter = localStateWriter;
            _remoteStateReader = remoteStateReader;
            _linkIdProvider = linkIdProvider;
            _loggerFactory = loggerFactory;
            Slog.Info($"state providers: local={localStateWriter} remote={remoteStateReader}");
        }

        protected override HandLink<TRemoteState> CreateClientLink(ITpReceiver receiver) 
            => new(this, receiver, _localStateWriter, _remoteStateReader, _linkIdProvider, _loggerFactory);

        protected override HandLink<TRemoteState> CreateServerLink(ITpLink innerLink) =>
            new(this, innerLink, _localStateWriter, _remoteStateReader,_linkIdProvider, _loggerFactory);

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