#if !UNITY_5_6_OR_NEWER
using System;
using Shared.Web;
using SIPSorcery.Net;

namespace Shared.Tp.Rtc.Sip
{
    public static class SipRtcExtensions
    {
        public static RtcIceCandidate ToShared(this RTCIceCandidate candidate)
        {
            //unfortunately SIP doesn't provide methods for conversion to RTCIceCandidateInit
            var candidateInitJson = candidate.toJSON();
            var result = WebSerializer.DeserializeObject<RtcIceCandidate>(candidateInitJson);
            return result;
        }

        public static RTCIceCandidateInit FromShared(this in RtcIceCandidate candidate)
        {
            return new()
            {
                candidate = candidate.candidate,
                sdpMid = candidate.sdpMid,
                sdpMLineIndex = candidate.sdpMLineIndex,
                usernameFragment = candidate.usernameFragment
            };
        }
        
        public static RtcSdpInit ToShared(this RTCSessionDescriptionInit sdp)
        {
            return new()
            {
                type = sdp.type.ToString(),
                sdp = sdp.sdp
            };
        }

        public static RTCSessionDescriptionInit FromShared(this in RtcSdpInit sdpInit)
        {
            var sharedType = sdpInit.type;
            if (!Enum.TryParse<RTCSdpType>(sharedType, out var sipType))
                throw new ArgumentException($"Unknown sdp type: {sharedType}");
            return new()
            {
                type = sipType,
                sdp = sdpInit.sdp
            };
        }
    }
}
#endif