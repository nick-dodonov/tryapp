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
        public ValueTask<RtcIceCandidate[]> SetAnswer(string token, RtcSdpInit answer, CancellationToken cancellationToken);
        public ValueTask AddIceCandidates(string token, string candidates, CancellationToken cancellationToken);
    }

    [Serializable]
    public struct RtcOffer
    {
        public int LinkId;
        public string LinkToken;

        public RtcSdpInit SdpInit;

        public RtcOffer(int linkId, string linkToken, in RtcSdpInit sdpInit)
        {
            LinkId = linkId;
            LinkToken = linkToken;
            SdpInit = sdpInit;
        }
    }

    [Serializable]
    public struct RtcSdpInit
    {
        //TODO: can be replaced with type / sdp
        public string Json;
        public RtcSdpInit(string json)
        {
            Json = json;
        }
    }

    [Serializable]
    public struct RtcIceCandidate
    {
        //TODO: can be replaced with candidate / sdpM* fields / another optional data
        public string Json;
        public RtcIceCandidate(string json)
        {
            Json = json;
        }
    }
}