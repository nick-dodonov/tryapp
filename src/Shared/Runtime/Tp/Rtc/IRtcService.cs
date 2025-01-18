using System;
using System.Threading;
using System.Threading.Tasks;
using Shared.Web;

namespace Shared.Tp.Rtc
{
    /// <summary>
    /// Interface for WebRTC signalling process
    /// Now it's implemented as:
    /// * REST-service via ASP WebApi as signalling service part
    /// * REST-client interlayer to access service
    /// * TODO: WebSocket implementation
    /// </summary>
    public interface IRtcService
    {
        public ValueTask<RtcOffer> GetOffer(CancellationToken cancellationToken);
        public ValueTask<RtcIceCandidate[]> SetAnswer(string token, RtcSdpInit answer, CancellationToken cancellationToken);
        public ValueTask AddIceCandidates(string token, RtcIceCandidate[] candidates, CancellationToken cancellationToken);
    }

    /// <summary>
    /// RtcOffer is answer from signalling service allowing to establish WebRTC link and control it via signalling
    /// </summary>
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

    /// <summary>
    /// RTCSessionDescriptionInit in implementations (JavaScript, SIPSorcery, Unity)  
    /// </summary>
    [Serializable]
    public struct RtcSdpInit
    {
        // ReSharper disable InconsistentNaming
        public string type;
        public string sdp;
        // ReSharper restore InconsistentNaming
        
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

    /// <summary>
    /// RTCIceCandidateInit in implementations (JavaScript, SIPSorcery, Unity)  
    /// </summary>
    [Serializable]
    public struct RtcIceCandidate
    {
        // ReSharper disable InconsistentNaming UnassignedField.Global UnusedMember.Global
        public string candidate;
        public string sdpMid;
        public ushort sdpMLineIndex;
        public string? usernameFragment;
        // ReSharper restore InconsistentNaming UnassignedField.Global UnusedMember.Global

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