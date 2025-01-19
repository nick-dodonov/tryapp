using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Shared.Log;
using Shared.Log.Asp;
using Shared.Tp;

namespace Common.Logic
{
    /// <summary>
    /// TODO: syn/ack for reliable initial state
    /// </summary>
    public class PeerLink : ExtLink
    {
        private readonly PeerApi _api = null!;
        private readonly ILoggerFactory _loggerFactory = null!;
        private ILogger _logger = null!;

        private string? _peerId;

        public PeerLink() { } //empty constructor only for generic usage

        public PeerLink(PeerApi api, ITpReceiver receiver, string peerId, ILoggerFactory loggerFactory) : base(receiver)
        {
            _api = api;
            _loggerFactory = loggerFactory;
            _logger = new IdLogger(loggerFactory.CreateLogger<PeerLink>(), peerId);
            _peerId = peerId;
        }

        public PeerLink(PeerApi api, ITpLink innerLink, ILoggerFactory loggerFactory) : base(innerLink)
        {
            _api = api;
            _loggerFactory = loggerFactory;
            _logger = new IdLogger(loggerFactory.CreateLogger<PeerLink>(), GetRemotePeerId());
        }

        public Task ConnectHandshake()
        {
            _logger.Info($"sending peer id: {_peerId}");
            var bytes = Encoding.UTF8.GetBytes(_peerId!);
            Send(bytes);

            //TODO: implement handshake await

            return Task.CompletedTask;
        }

        public sealed override string GetRemotePeerId() =>
            $"{_peerId}/{InnerLink.GetRemotePeerId()}"; //TODO: speedup without string interpolation

        public override void Received(ITpLink link, byte[]? bytes)
        {
            if (_peerId == null && bytes != null)
            {
                _peerId = Encoding.UTF8.GetString(bytes);
                _logger = new IdLogger(_loggerFactory.CreateLogger<PeerLink>(), GetRemotePeerId());
                _logger.Info($"received peer id: {_peerId}");

                Receiver = _api.CallConnected(this);
                return;
            }

            base.Received(link, bytes);
        }
    }
}