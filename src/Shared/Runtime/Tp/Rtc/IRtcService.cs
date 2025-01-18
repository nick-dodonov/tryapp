using System;
using System.Threading;
using System.Threading.Tasks;

namespace Shared.Tp.Rtc
{
    /// <summary>
    /// TODO: make different implementations (not only current REST variant but WebSocket too)
    /// </summary>
    public interface IRtcService
    {
        //TODO: shared RTC types for SDP (offer, answer) and ICE candidates
        public ValueTask<RtcOffer> GetOffer(CancellationToken cancellationToken);
        public ValueTask<string> SetAnswer(string id, string answer, CancellationToken cancellationToken);
        public ValueTask AddIceCandidates(string id, string candidates, CancellationToken cancellationToken);
    }
    
    [Serializable]
    public struct RtcOffer
    {
        public string LinkId; //TODO: possibly we can use ice-ufrag from RTCSessionDescriptionInit in sdp
        public string SdpInitJson;

        public RtcOffer(string linkId, string sdpInitJson)
        {
            LinkId = linkId;
            SdpInitJson = sdpInitJson;
        }
    }
}