using System;
using System.Threading;
using System.Threading.Tasks;
using Shared.Web;

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
        public ValueTask AddIceCandidates(string token, RtcIceCandidate[] candidates, CancellationToken cancellationToken);
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

        public override string ToString() => $"{nameof(RtcOffer)}({LinkId} '{LinkToken}' {SdpInit})";
    }

    [Serializable]
    public struct RtcSdpInit
    {
        // ReSharper disable UnusedMember.Global
        public string type;
        public string sdp;
        // ReSharper restore UnusedMember.Global
        
        public override string ToString() => $"{nameof(RtcSdpInit)}({ToJson()})";
        
        private string? _json;
        public string ToJson() => _json ??= WebSerializer.SerializeObject(this);

        public static RtcSdpInit FromJson(string json)
        {
            var result = WebSerializer.DeserializeObject<RtcSdpInit>(json);
            result._json = json;
            return result;
        }
    }

    [Serializable]
    public struct RtcIceCandidate //TODO: rename to RtcIceCandidateInit
    {
        // ReSharper disable UnusedMember.Global
        public string candidate;
        public string sdpMid;
        public ushort sdpMLineIndex;
        public string usernameFragment;
        // ReSharper restore UnusedMember.Global

        public override string ToString() => $"{nameof(RtcIceCandidate)}({ToJson()})";

        private string? _json;
        public string ToJson() => _json ??= WebSerializer.SerializeObject(this);

        public static RtcIceCandidate FromJson(string json)
        {
            var result = WebSerializer.DeserializeObject<RtcIceCandidate>(json);
            result._json = json;
            return result;
        }
    }
}