using Shared.Log;
using Shared.Session;
using Shared.Web;
using SIPSorcery.Net;

namespace Server.Rtc;

internal class SipRtcLink(SipRtcService service, string id, RTCPeerConnection peerConnection, ILoggerFactory loggerFactory)
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
            _logger.LogDebug($"onicecandidate: {candidate}");
            // if (candidate.type == RTCIceCandidateType.host)
            // {
            //     _logger.LogDebug("onicecandidate: skip host candidate");
            //     return;
            // }
            _iceCandidates.Add(candidate);
        };
        peerConnection.onicecandidateerror += (candidate, error) => _logger.LogWarning($"onicecandidateerror: '{error}' {candidate}");
        peerConnection.oniceconnectionstatechange += state => _logger.LogDebug($"oniceconnectionstatechange: {state}");
        peerConnection.onicegatheringstatechange += state =>
        {
            _logger.LogDebug($"onicegatheringstatechange: {state}");
            if (state == RTCIceGatheringState.complete)
                IceCollectCompleteTcs.SetResult(_iceCandidates);
        };
        
        peerConnection.onconnectionstatechange += state =>
        {
            _logger.LogDebug($"onconnectionstatechange: state changed to {state}");
            if (state is
                RTCPeerConnectionState.closed or
                RTCPeerConnectionState.disconnected or
                RTCPeerConnectionState.failed)
            {
                _logger.LogDebug($"onconnectionstatechange: Peer {id} closing");
                service.RemoveLink(id);
                IceCollectCompleteTcs.TrySetCanceled();
                peerConnection.close();
            }
            else if (state == RTCPeerConnectionState.connected)
                _logger.LogDebug("onconnectionstatechange: connected");
        };
        
        var channel = DataChannel;
        channel.onopen += () =>
        {
            _logger.LogDebug($"DataChannel: onopen: label={channel.label}");
            service.StartLinkLogic(channel, peerConnection);
        };
        channel.onmessage += (_, _, data) =>
        {
            //_logger.LogDebug($"DataChannel: onmessage: type={type} data=[{data.Length}]");
            var str = System.Text.Encoding.UTF8.GetString(data);
            _logger.LogDebug($"DataChannel: onmessage: {str}");
            try
            {
                LastClientState = WebSerializer.DeserializeObject<ClientState>(str);
            }
            catch (Exception e)
            {
                _logger.LogError($"DataChannel: onmessage: failed to deserialize: {e}");
            }
        };
        channel.onclose += () => _logger.LogDebug($"DataChannel: onclose: label={channel.label}");
        channel.onerror += error => _logger.LogError($"DataChannel: error: {error}");
    }

    private class PeerIdLogger(ILogger inner, string peerId) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => inner.BeginScope(state);
        public bool IsEnabled(LogLevel logLevel) => inner.IsEnabled(logLevel);
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel)) return;
            var message = formatter(state, exception);
            var prefixedMessage = $"{message} PeerId={peerId}";
            inner.Log(logLevel, eventId, prefixedMessage, exception, (m, _) => m);
        }
    }
}