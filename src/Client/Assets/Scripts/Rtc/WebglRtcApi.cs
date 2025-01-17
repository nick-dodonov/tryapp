#nullable enable
using System;
using System.Threading;
using System.Threading.Tasks;
using Shared.Log;
using Shared.Rtc;

namespace Client.Rtc
{
    /// <summary>
    /// Interaction with browser scripting:
    ///     https://docs.unity3d.com/Manual/webgl-interactingwithbrowserscripting.html
    ///     https://discussions.unity.com/t/send-byte-array-from-js-to-unity/874743/18
    /// </summary>
    public class WebglRtcApi : IRtcApi
    {
        private static readonly Slog.Area _log = new();

        private readonly IRtcService _service;

        public WebglRtcApi(IRtcService service)
        {
            _log.Info(".");
            _service = service;
            WebglRtcNative.RtcInit(
                WebglRtcLink.ConnectAnswerCallback,
                WebglRtcLink.ConnectCandidatesCallback,
                WebglRtcLink.ConnectCompleteCallback,
                WebglRtcLink.ReceivedCallback
            );
        }

        async Task<IRtcLink> IRtcApi.Connect(IRtcReceiver receiver, CancellationToken cancellationToken)
        {
            _log.Info(".");
            var link = new WebglRtcLink(_service, receiver);
            await link.Connect(cancellationToken);
            return link;
        }

        void IRtcApi.Listen(IRtcListener listener)
            => throw new NotSupportedException("server side not implemented");
    }
}