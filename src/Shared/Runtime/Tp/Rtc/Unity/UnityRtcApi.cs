#if UNITY_5_6_OR_NEWER && (UNITY_EDITOR || !UNITY_WEBGL)
using System;
using System.Threading;
using System.Threading.Tasks;
using Shared.Log;
using Shared.Web;
using Unity.WebRTC;

namespace Shared.Tp.Rtc.Unity
{
    public class UnityRtcApi : ITpApi
    {
        private static readonly Slog.Area _log = new();
        
        private readonly IRtcService _service;

        public UnityRtcApi(IRtcService service)
        {
            _log.Info(".");
            //Disabled because Unity Editor crashes (macOS)
            //WebRTC.ConfigureNativeLogging(true, NativeLoggingSeverity.Info);

            _service = service;
        }

        async Task<ITpLink> ITpApi.Connect(ITpReceiver receiver, CancellationToken cancellationToken)
        {
            var link = new UnityRtcLink(_service, receiver);
            await link.Connect(cancellationToken);
            return link;
        }

        void ITpApi.Listen(ITpListener listener) => throw new NotSupportedException("server side not implemented");
    }
}
#endif
