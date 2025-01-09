using Shared.Session;
using SIPSorcery.Net;

namespace Server.Rtc;

internal class SipRtcLink(RTCPeerConnection peerConnection)
{
    public readonly RTCPeerConnection PeerConnection = peerConnection;
    public readonly List<RTCIceCandidate> IceCandidates = [];
    public readonly TaskCompletionSource<List<RTCIceCandidate>> IceCollectCompleteTcs = new();
    public RTCDataChannel? DataChannel;
    public ClientState LastClientState;
}