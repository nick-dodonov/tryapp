using System.Text;
using Shared.Log;
using Shared.Rtc;
using Shared.Session;
using Shared.Web;
using SIPSorcery.Net;

namespace Server.Rtc;

internal class SipRtcLink(
    SipRtcService service,
    string id,
    RTCPeerConnection peerConnection,
    ILoggerFactory loggerFactory)
    : IRtcLink
{
    private readonly ILogger _logger = new PeerIdLogger(loggerFactory.CreateLogger<SipRtcLink>(), id);

    public RTCPeerConnection PeerConnection => peerConnection;

    private readonly List<RTCIceCandidate> _iceCandidates = [];
    public readonly TaskCompletionSource<List<RTCIceCandidate>> IceCollectCompleteTcs = new();
    public RTCDataChannel? DataChannel;
    public ClientState LastClientState;

    public async Task Init()
    {
        _logger.Info(".");
        DataChannel = await peerConnection.createDataChannel("test", new()
        {
            ordered = false,
            maxRetransmits = 0
        });

        peerConnection.onicecandidate += candidate =>
        {
            _logger.Info($"onicecandidate: {candidate}");
            // if (candidate.type == RTCIceCandidateType.host)
            // {
            //     _logger.Info("onicecandidate: skip host candidate");
            //     return;
            // }
            _iceCandidates.Add(candidate);
        };
        peerConnection.onicecandidateerror += (candidate, error) =>
            _logger.Warn($"onicecandidateerror: '{error}' {candidate}");
        peerConnection.oniceconnectionstatechange += state => 
            _logger.Info($"oniceconnectionstatechange: {state}");
        peerConnection.onicegatheringstatechange += state =>
        {
            _logger.Info($"onicegatheringstatechange: {state}");
            if (state == RTCIceGatheringState.complete)
                IceCollectCompleteTcs.SetResult(_iceCandidates);
        };

        peerConnection.onconnectionstatechange += state =>
        {
            _logger.Info($"onconnectionstatechange: state changed to {state}");
            if (state is
                RTCPeerConnectionState.closed or
                RTCPeerConnectionState.disconnected or
                RTCPeerConnectionState.failed)
            {
                ((IDisposable)this).Dispose(); //TODO: replace with just notification to dispose outside
            }
            else if (state == RTCPeerConnectionState.connected)
                _logger.Info("onconnectionstatechange: connected");
        };

        var channel = DataChannel;
        channel.onopen += () =>
        {
            _logger.Info($"DataChannel: onopen: label={channel.label}");
            service.StartLinkLogic(this, channel, peerConnection);
        };
        channel.onmessage += (_, _, data) =>
        {
            //_logger.Info($"DataChannel: onmessage: type={type} data=[{data.Length}]");
            var str = Encoding.UTF8.GetString(data);
            _logger.Info($"DataChannel: onmessage: {str}");
            try
            {
                LastClientState = WebSerializer.DeserializeObject<ClientState>(str);
            }
            catch (Exception e)
            {
                _logger.Error($"DataChannel: onmessage: failed to deserialize: {e}");
            }
        };
        channel.onclose += () => _logger.Info($"DataChannel: onclose: label={channel.label}");
        channel.onerror += error => _logger.Error($"DataChannel: error: {error}");
    }

    void IDisposable.Dispose()
    {
        _logger.Info(".");
        service.RemoveLink(id);
        IceCollectCompleteTcs.TrySetCanceled();
        peerConnection.close();
        throw new NotImplementedException();
    }

    void IRtcLink.Send(byte[] bytes)
    {
        var content = Encoding.UTF8.GetString(bytes);
        _logger.Info($"[{bytes.Length}]: {content}");
        DataChannel?.send(bytes);
    }
}