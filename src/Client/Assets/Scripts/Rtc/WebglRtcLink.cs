#nullable enable
using System.Threading;
using System.Threading.Tasks;
using Shared.Log;
using Shared.Rtc;

namespace Client.Rtc
{
    public class WebglRtcLink : BaseRtcLink
    {
        private static readonly Slog.Area _log = new();
        
        private readonly WebglRtcApi _api;

        private int _peerId = -1;
        public int PeerId => _peerId;

        public WebglRtcLink(WebglRtcApi api, IRtcService service, IRtcReceiver receiver)
            : base(service, receiver)
        {
            _api = api;
        }

        public override void Dispose()
        {
            _log.Info(".");
            if (_peerId >= 0)
            {
                _api.Remove(this);

                WebglRtcNative.RtcClose(_peerId);
                _peerId = -1;
            }
        }

        public override void Send(byte[] bytes)
        {
            //_log.Info($"{bytes.Length} bytes");
            WebglRtcNative.RtcSend(_peerId, bytes, bytes.Length);
        }

        public async Task Connect(CancellationToken cancellationToken)
        {
            var offerStr = await ObtainOffer(cancellationToken);
            _log.Info("requesting");
            _peerId = WebglRtcNative.RtcConnect(offerStr);
            _log.Info($"result peerId={_peerId}");
        }
    }
}