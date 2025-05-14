using System;
using System.Linq;

namespace Shared.Tp.Rtc.Sip
{
    [Serializable]
    public class SipRtcConfig : RtcConfig
    {
        /// <summary>
        /// Suggested remote rtc peer connection configuration.
        /// It allows to set up the same ICE servers on remote peer (restricted cone / symmetric NATs hole punching)  
        /// </summary>
        public RtcConfig? RemoteConfig { get; set; }
        
        //TODO: SIPSorcery.Net.RTCPeerConnection port range settings here

        public override string ToString() =>
            $"{nameof(SipRtcConfig)}({string.Join(", ", IceServers?.Select(x => $"\"{x.Urls?[0]}\"") ?? Enumerable.Empty<string>())} Remote={RemoteConfig})";
    }
}
