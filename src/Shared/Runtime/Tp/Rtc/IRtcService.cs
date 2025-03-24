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
        public ValueTask<RtcIcInit[]> SetAnswer(string token, RtcSdpInit answer, CancellationToken cancellationToken);
        public ValueTask AddIceCandidates(string token, RtcIcInit[] candidates, CancellationToken cancellationToken);
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

        /// <summary>
        /// Suggested rtc peer connection configuration.
        /// It allows to set up the same ICE servers as remote peer (restricted cone / symmetric NATs hole punching)  
        /// </summary>
        public RtcConfig? Config;

        public override string ToString() => $"{nameof(RtcOffer)}({LinkId} '{LinkToken}' {SdpInit}{(Config != null ? $" {Config}" : null)})";
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
        public string ToJson() => _json ??= WebSerializer.Default.Serialize(this);

        public static RtcSdpInit FromJson(string json)
        {
            var result = WebSerializer.Default.Deserialize<RtcSdpInit>(json);
            result._json = json;
            return result;
        }
    }

    /// <summary>
    /// RTCIceCandidateInit in implementations (JavaScript, SIPSorcery, Unity)  
    /// </summary>
    [Serializable]
    public struct RtcIcInit
    {
        // ReSharper disable InconsistentNaming UnassignedField.Global UnusedMember.Global
        public string candidate;
        public string sdpMid;
        public ushort sdpMLineIndex;
        public string? usernameFragment;
        // ReSharper restore InconsistentNaming UnassignedField.Global UnusedMember.Global

        public override string ToString() => $"{nameof(RtcIcInit)}({ToJson()})";

        private string? _json;
        public string ToJson() => _json ??= WebSerializer.Default.Serialize(this);

        public static RtcIcInit FromJson(string json)
        {
            var result = WebSerializer.Default.Deserialize<RtcIcInit>(json);
            result._json = json;
            return result;
        }
    }
}