using System.Text;
using Shared.Log;
using Shared.Rtc;
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
    internal IRtcReceiver? Receiver { get; set; }

    private readonly List<RTCIceCandidate> _iceCandidates = [];
    public readonly TaskCompletionSource<List<RTCIceCandidate>> IceCollectCompleteTcs = new();
    private RTCDataChannel? _dataChannel;

    public async Task Init()
    {
        _logger.Info(".");
        _dataChannel = await peerConnection.createDataChannel("test", new()
        {
            ordered = false,
            maxRetransmits = 0
        });

        peerConnection.onicecandidate += candidate =>
        {
            _logger.Info($"onicecandidate: {candidate}");
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

        var channel = _dataChannel;
        channel.onopen += () =>
        {
            _logger.Info($"DataChannel: onopen: label={channel.label}");
            service.StartLinkLogic(this);
        };
        channel.onmessage += (_, _, data) =>
        {
            var str = Encoding.UTF8.GetString(data);
            _logger.Info($"DataChannel: onmessage: {str}");
            Receiver?.Received(this, data);
        };
        channel.onclose += () =>
        {
            _logger.Info($"DataChannel: onclose: label={channel.label}");
            Receiver?.Received(this, null);
        };
        channel.onerror += error => 
            _logger.Error($"DataChannel: onerror: {error}");
    }

    void IDisposable.Dispose()
    {
        _logger.Info(".");
        service.RemoveLink(id);
        IceCollectCompleteTcs.TrySetCanceled();
        peerConnection.close();
    }

    void IRtcLink.Send(byte[] bytes)
    {
        if (_dataChannel?.readyState != RTCDataChannelState.open)
        {
            _logger.Warn($"skip: readyState={_dataChannel?.readyState}");
            return;
        }
        if (peerConnection.connectionState != RTCPeerConnectionState.connected)
        {
            _logger.Info($"skip: connectionState={peerConnection.connectionState}");
            return;
        }
        if (peerConnection.sctp.state != RTCSctpTransportState.Connected)
        {
            _logger.Info($"skip: sctp.state={peerConnection.sctp.state}");
            return;
        }
        
        var content = Encoding.UTF8.GetString(bytes);
        _logger.Info($"[{bytes.Length}]: {content}");
        _dataChannel?.send(bytes);
    }
}