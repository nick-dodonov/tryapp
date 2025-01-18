#if !UNITY_5_6_OR_NEWER
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
    }
}
#endif