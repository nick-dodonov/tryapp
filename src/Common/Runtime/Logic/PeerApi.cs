using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Shared.Log;
using Shared.Tp;

namespace Common.Logic
{
    /// <summary>
    /// TODO: custom initial state (not only peer id is required for logic)
    /// </summary>
    public class PeerApi : ExtApi<PeerLink>
    {
        private readonly ILoggerFactory _loggerFactory;

        private readonly string _peerId;

        public PeerApi(ITpApi innerApi, string peerId, ILoggerFactory loggerFactory) : base(innerApi)
        {
            _loggerFactory = loggerFactory;
            var logger = loggerFactory.CreateLogger<PeerApi>();
            logger.Info(peerId);
            _peerId = peerId;
        }

        protected override PeerLink CreateClientLink(ITpReceiver receiver) =>
            new(this, receiver, _peerId, _loggerFactory);

        protected override PeerLink CreateServerLink(ITpLink innerLink) =>
            new(this, innerLink, _loggerFactory);

        public override async ValueTask<ITpLink> Connect(ITpReceiver receiver, CancellationToken cancellationToken)
        {
            var link = (PeerLink)await base.Connect(receiver, cancellationToken);
            await link.ConnectHandshake();
            return link;
        }

        public override ITpReceiver Connected(ITpLink link)
        {
            var extLink = CreateServerLink(link);
            return extLink;
        }

        internal ITpReceiver? CallConnected(PeerLink link) 
            => Listener?.Connected(link);
    }
}