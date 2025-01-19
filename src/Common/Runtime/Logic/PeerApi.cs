using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Shared.Log;
using Shared.Tp;

namespace Common.Logic
{
    public class PeerApi : ExtApi<PeerLink>
    {
        private readonly ILogger _logger;

        private readonly string _peerId;
        public PeerApi(ITpApi innerApi, string peerId, ILoggerFactory loggerFactory) : base(innerApi)
        {
            _logger = loggerFactory.CreateLogger<PeerApi>();
            _logger.Info(peerId);
            _peerId = peerId;
        }

        protected override PeerLink CreateClientLink(ITpReceiver receiver) => new(receiver, _peerId);
        public override async Task<ITpLink> Connect(ITpReceiver receiver, CancellationToken cancellationToken)
        {
            var link = await base.Connect(receiver, cancellationToken);
            
            _logger.Info($"sending: {_peerId}");
            var bytes = Encoding.UTF8.GetBytes(_peerId);
            link.Send(bytes);
            return link;
        }
    }

    public class PeerLink : ExtLink
    {
        private readonly string? _peerId;
        public PeerLink() { }
        public PeerLink(ITpReceiver receiver, string? peerId)
        {
            Receiver = receiver;
            _peerId = peerId;
        }

        //TODO: speedup without string interpolation
        public override string GetRemotePeerId() => $"{_peerId}<{base.GetRemotePeerId()}>";
    }
}