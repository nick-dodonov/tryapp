#nullable enable
using System.Collections.Generic;
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

        //pin managed object as pointer in native implementation (speedup managed object association within callbacks)
        private GCHandle _managedHandle;
        private int _nativeHandle = -1;

        private static readonly Dictionary<int, WebglRtcLink> Links = new(); //TODO: pass instance to callbacks

        public static bool TryGetLink(int nativeHandle, out WebglRtcLink link)
            => Links.TryGetValue(nativeHandle, out link);

        public WebglRtcLink(IRtcService service, IRtcReceiver receiver)
            : base(service, receiver)
        {
            _managedHandle = GCHandle.Alloc(this);
        }

        public override void Dispose()
        {
            var allocated = _managedHandle.IsAllocated;
            _log.Info($"allocated: {allocated}");
            if (allocated)
            {
                Links.Remove(_nativeHandle);

                WebglRtcNative.RtcClose(_nativeHandle);
                _nativeHandle = -1;

                _managedHandle.Free();
            }
        }

        public override void Send(byte[] bytes)
        {
            //_log.Info($"{bytes.Length} bytes");
            WebglRtcNative.RtcSend(_nativeHandle, bytes, bytes.Length);
        }

        public async Task Connect(CancellationToken cancellationToken)
        {
            var offerStr = await ObtainOffer(cancellationToken);
            _log.Info("requesting");
            _nativeHandle = WebglRtcNative.RtcConnect(offerStr);
            _log.Info($"result peerId={_nativeHandle}");
            Links.Add(_nativeHandle, this);
        }
    }
}