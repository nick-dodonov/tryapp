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
            new(receiver, _peerId, _loggerFactory);

        protected override PeerLink CreateServerLink(ITpLink innerLink) =>
            new PeerLink(innerLink, _loggerFactory).InitPeerLogger();

        public override async Task<ITpLink> Connect(ITpReceiver receiver, CancellationToken cancellationToken)
        {
            var link = (PeerLink)await base.Connect(receiver, cancellationToken);
            await link.ConnectHandshake();
            return link;
        }
    }

    public class PeerLink : ExtLink
    {
        private readonly ILoggerFactory _loggerFactory = null!;
        private ILogger _logger = null!;
        private string? _peerId;

        public PeerLink()
        {
        } //empty constructor only for generic usage

        public PeerLink(ITpReceiver receiver, string peerId, ILoggerFactory loggerFactory) : base(receiver)
        {
            _loggerFactory = loggerFactory;
            _peerId = peerId;
            _logger = new IdLogger(loggerFactory.CreateLogger<PeerLink>(), _peerId);
        }

        public PeerLink(ITpLink innerLink, ILoggerFactory loggerFactory) : base(innerLink)
            => _loggerFactory = loggerFactory;

        public PeerLink InitPeerLogger() // separate init allows to use virtual methods
        {
            _logger = new IdLogger(_loggerFactory.CreateLogger<PeerLink>(), GetRemotePeerId());
            return this;
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
                _peerId = Encoding.UTF8.GetString(bytes);
                InitPeerLogger();
                _logger.Info($"received peer id: {_peerId}");
                return;
            }

            base.Received(link, bytes);
        }
    }
}