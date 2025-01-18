#if UNITY_5_6_OR_NEWER && (UNITY_EDITOR || !UNITY_WEBGL)
using System;
using Unity.WebRTC;

namespace Shared.Tp.Rtc.Unity
{
    public static class UnityRtcExtensions
    {
        public static RtcIceCandidate ToShared(this RTCIceCandidateInit candidate)
        {
            var unityLineIndex = candidate.sdpMLineIndex;
            if (unityLineIndex > ushort.MaxValue)
                throw new ArgumentException($"sdpMLineIndex too big: {unityLineIndex}");
            
            var sharedLineIndex = (ushort)(unityLineIndex ?? 0);
            return new()
            {
                candidate = candidate.candidate,
                sdpMid = candidate.sdpMid,
                sdpMLineIndex = sharedLineIndex
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

        public static RtcSdpInit ToShared(this RTCSessionDescription sdp)
        {
            return new()
            {
                type = sdp.type.ToString().ToLower(),
                sdp = sdp.sdp
            };
        }

        public static RTCSessionDescription FromShared(this in RtcSdpInit sdpInit)
        {
            var sharedType = sdpInit.type;
            if (!Enum.TryParse<RTCSdpType>(sharedType, true, out var unityType))
                throw new ArgumentException($"Unknown sdp type: {sharedType}");
            return new()
            {
                type = unityType,
                sdp = sdpInit.sdp
            };
        }
    }
}
#endif