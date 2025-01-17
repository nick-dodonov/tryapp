#if UNITY_5_6_OR_NEWER
#if UNITY_EDITOR || !UNITY_WEBGL
#nullable enable
using System;
using System.Threading;
using System.Threading.Tasks;
using Shared.Log;
using Shared.Tp;
using Shared.Tp.Rtc;
using Shared.Web;
using Unity.WebRTC;

namespace Client.Rtc
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

    public static class UnityRtcDebug
    {
        public static string Describe(RTCDataChannel channel) 
            => $"id={channel.Id} label={channel.Label} ordered={channel.Ordered} maxRetransmits={channel.MaxRetransmits} protocol={channel.Protocol} negotiated={channel.Negotiated} bufferedAmount={channel.BufferedAmount} readyState={channel.ReadyState}";
        public static string Describe(in RTCSessionDescription description)
            => WebSerializer.SerializeObject(description); //$"type={description.type} sdp={description.sdp}";
        public static string Describe(RTCIceCandidateInit candidate)
            => $"candidate=\"{candidate.candidate}\" sdpMid={candidate.sdpMid} sdpMLineIndex={candidate.sdpMLineIndex}";
        public static string Describe(RTCIceCandidate candidate)
            => $"address={candidate.Address} port={candidate.Port} protocol={candidate.Protocol} candidate=\"{candidate.Candidate}\"";
    }
}
#endif
#endif