#nullable enable
using System.Runtime.InteropServices;
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

        [DllImport("__Internal")]
        private static extern int RtcConnect(string offer);
        [DllImport("__Internal")]
        private static extern void RtcClose(int peerId);
        [DllImport("__Internal")]
        private static extern void RtcSend(int peerId, byte[] bytes, int size);

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

                RtcClose(_peerId);
                _peerId = -1;
            }
        }

        public override void Send(byte[] bytes)
        {
            //_log.Info($"{bytes.Length} bytes");
            RtcSend(_peerId, bytes, bytes.Length);
        }

        public async Task Connect(CancellationToken cancellationToken)
        {
            var offerStr = await ObtainOffer(cancellationToken);
            _log.Info("requesting");
            _peerId = RtcConnect(offerStr);
            _log.Info($"result peerId={_peerId}");
        }
    }
}