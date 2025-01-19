using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Shared.Log;
using Shared.Log.Asp;
using Shared.Tp;

namespace Common.Logic
{
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
            new(receiver, _peerId, new IdLogger(_loggerFactory.CreateLogger<PeerLink>(), _peerId));

        protected override PeerLink CreateServerLink(ITpLink innerLink) =>
            new(innerLink, new IdLogger(_loggerFactory.CreateLogger<PeerLink>(), _peerId));

        public override async Task<ITpLink> Connect(ITpReceiver receiver, CancellationToken cancellationToken)
        {
            var link = (PeerLink)await base.Connect(receiver, cancellationToken);
            await link.ConnectHandshake();
            return link;
        }
    }

    public class PeerLink : ExtLink
    {
        private readonly ILogger _logger;
        private string? _peerId;

        public PeerLink() => _logger = null!; //empty constructor only for generic usage

        public PeerLink(ITpReceiver receiver, string peerId, ILogger logger)
        {
            _logger = logger;
            Receiver = receiver;
            _peerId = peerId;
        }

        public PeerLink(ITpLink innerLink, ILogger logger)
        {
            _logger = logger;
            InnerLink = innerLink;
        }

        public Task ConnectHandshake()
        {
            _logger.Info($"sending peer id: {_peerId}");
            var bytes = Encoding.UTF8.GetBytes(_peerId!);
            Send(bytes);
            
            //TODO: implement handshake await
            
            return Task.CompletedTask;
        }

        public override string GetRemotePeerId() =>
            $"{_peerId}<{base.GetRemotePeerId()}>"; //TODO: speedup without string interpolation

        public override void Received(ITpLink link, byte[]? bytes)
        {
            if (_peerId == null && bytes != null)
            {
                _logger.Info($"received peer id: {bytes}");
                _peerId = Encoding.UTF8.GetString(bytes);
                return;
            }

            base.Received(link, bytes);
        }
    }
}