#if UNITY_5_6_OR_NEWER && (UNITY_EDITOR || !UNITY_WEBGL)
using Unity.WebRTC;

namespace Shared.Tp.Rtc.Unity
{
    public static class UnityRtcExtensions
    {
        public static RtcIceCandidate ToShared(this RTCIceCandidateInit candidate)
        {
            var sdpMLineIndex = candidate.sdpMLineIndex ?? 0;
            return new()
            {
                candidate = candidate.candidate,
                sdpMid = candidate.sdpMid,
                sdpMLineIndex = (ushort)sdpMLineIndex //TODO: debug assert ushort enough
            };
        }
        
        public static RTCIceCandidateInit FromShared(this in RtcIceCandidate candidate)
        {
            return new()
            {
                candidate = candidate.candidate,
                sdpMid = candidate.sdpMid,
                sdpMLineIndex = candidate.sdpMLineIndex
            };
        }
    }
}
#endif