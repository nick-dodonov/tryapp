using System.Collections.Concurrent;
using System.Net;
using Shared.Rtc;
using SIPSorcery.Net;
using TinyJson;

namespace Server.Rtc;

/// <summary>
/// Based on several samples
///     examples/WebRTCExamples/WebRTCAspNet
///     examples/WebRTCExamples/WebRTCGetStartedDataChannel
///     https://www.marksort.com/udp-like-networking-in-the-browser/
/// </summary>
public class SipRtcService : IRtcService, IHostedService
{
    private readonly ILogger<SipRtcService> _logger;

    private class Connection(RTCPeerConnection peerConnection)
    {
        public readonly RTCPeerConnection PeerConnection = peerConnection;
        public readonly List<RTCIceCandidate> IceCandidates = [];
        public readonly TaskCompletionSource<bool> IceCollectCompleteTcs = new();
        public RTCDataChannel? DataChannel;
    }
    private readonly ConcurrentDictionary<string, Connection> _connections = new();

    /// <summary>
    /// Based on several samples
    ///     examples/WebRTCExamples/WebRTCAspNet
    ///     examples/WebRTCExamples/WebRTCGetStartedDataChannel
    ///     https://www.marksort.com/udp-like-networking-in-the-browser/
    /// </summary>
    public SipRtcService(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<SipRtcService>();
        SIPSorcery.LogFactory.Set(loggerFactory);
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("StartAsync");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("StopAsync");
        return Task.CompletedTask;
    }

    public async ValueTask<string> GetOffer(string id, CancellationToken cancellationToken) 
        => (await GetOffer(id)).toJSON();

    public ValueTask<string> SetAnswer(string id, string answerJson, CancellationToken cancellationToken)
    {
        if (!RTCSessionDescriptionInit.TryParse(answerJson, out var answer))
            throw new InvalidOperationException("Body must contain SDP answer for id: {id}");
        return SetAnswer(id, answer, cancellationToken);
    }

    private async Task<RTCSessionDescriptionInit> GetOffer(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentNullException(nameof(id), "ID must be supplied to create new peer connection");
        if (_connections.ContainsKey(id))
            throw new ArgumentNullException(nameof(id), "ID is already in use");

        _logger.LogDebug($"creating RTCPeerConnection and RTCDataChannel for id={id}");
        // const string STUN_URL = "stun:stun.sipsorcery.com";
        // var config = new RTCConfiguration {
        //     iceServers = [new() { urls = STUN_URL }]
        // };
        //var peerConnection = new RTCPeerConnection(config);
        var peerConnection = new RTCPeerConnection();
        var connection = new Connection(peerConnection)
        {
            DataChannel = await peerConnection.createDataChannel("test", new()
            {
                ordered = false,
                maxRetransmits = 0
            })
        };

        peerConnection.onicecandidate += candidate =>
        {
            _logger.LogDebug($"onicecandidate: {candidate}");
            connection.IceCandidates.Add(candidate);
        };
        peerConnection.onicecandidateerror += (candidate, error) =>
            _logger.LogWarning($"onicecandidateerror: '{error}' {candidate}");
        peerConnection.oniceconnectionstatechange += state => 
            _logger.LogDebug($"oniceconnectionstatechange: {state}");
        peerConnection.onicegatheringstatechange += state =>
        {
            _logger.LogDebug($"onicegatheringstatechange: {state}");
            if (state == RTCIceGatheringState.complete)
                connection.IceCollectCompleteTcs.SetResult(true);
        };
        
        peerConnection.OnRtpPacketReceived += (IPEndPoint rep, SDPMediaTypesEnum media, RTPPacket rtpPkt) 
            => _logger.LogDebug($"OnRtpPacketReceived: RTP {media} pkt received, SSRC {rtpPkt.Header.SyncSource}, SeqNum {rtpPkt.Header.SequenceNumber}");
        peerConnection.OnReceiveReport += (IPEndPoint ip, SDPMediaTypesEnum media, RTCPCompoundPacket pkt) 
            => _logger.LogDebug($"OnReceiveReport: RTP {media}");
        peerConnection.OnSendReport += (SDPMediaTypesEnum media, RTCPCompoundPacket pkt) 
            => _logger.LogDebug($"OnSendReport: RTP {media}");
        peerConnection.OnTimeout += media 
            => _logger.LogWarning($"OnTimeout: {media}");
        
        peerConnection.onconnectionstatechange += state =>
        {
            _logger.LogDebug($"onconnectionstatechange: Peer {id} state changed to {state}");
            if (state is 
                RTCPeerConnectionState.closed or 
                RTCPeerConnectionState.disconnected or 
                RTCPeerConnectionState.failed)
                _connections.TryRemove(id, out _);
            else if (state == RTCPeerConnectionState.connected)
                _logger.LogDebug("onconnectionstatechange: Peer connection connected");
        };
        
        var channel = connection.DataChannel;
        channel.onopen += () =>
        {
            _logger.LogDebug($"DataChannel: onopen: label={channel.label}");
            
            var frameId = 1;
            var timer = new System.Timers.Timer(1000); // Timer interval set to 1 second
            timer.Elapsed += (sender, e) =>
            {
                if (channel.readyState != RTCDataChannelState.open)
                {
                    _logger.LogDebug($"DataChannel: timer: stop: readyState={channel.readyState}");
                    timer.Stop();
                    return;
                }
                if (peerConnection.connectionState != RTCPeerConnectionState.connected)
                {
                    _logger.LogDebug($"DataChannel: timer: stop: connectionState={peerConnection.connectionState}");
                    timer.Stop();
                    return;
                }
                if (peerConnection.sctp.state != RTCSctpTransportState.Connected)
                {
                    _logger.LogDebug($"DataChannel: timer: stop: sctp.state={peerConnection.sctp.state}");
                    timer.Stop();
                    return;
                }

                var message = $"{frameId++};TODO-FROM-SERVER;{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
                _logger.LogDebug($"DataChannel: sending: {message}");
                channel.send(message);
            };
            timer.Start();
        };
        channel.onmessage += (datachannel, type, data) =>
        {
            //_logger.LogDebug($"DataChannel: onmessage: type={type} data=[{data.Length}]");
            var str = System.Text.Encoding.UTF8.GetString(data);
            _logger.LogDebug($"DataChannel: onmessage: {str}");
        };
        channel.onclose += () => _logger.LogDebug($"DataChannel: onclose: label={channel.label}");
        channel.onerror += error => _logger.LogError($"DataChannel: error: {error}");
        
        _logger.LogDebug($"creating offer for id={id}");
        var offerSdp = peerConnection.createOffer();
        await peerConnection.setLocalDescription(offerSdp);

        _connections.TryAdd(id, connection);

        _logger.LogDebug($"returning offer for id={id}: {offerSdp}");
        return offerSdp;
    }

    private async ValueTask<string> SetAnswer(string id, RTCSessionDescriptionInit description, CancellationToken cancellationToken)
    {
        if (!_connections.TryGetValue(id, out var connection))
            throw new ApplicationException($"No peer connection is available for the specified id: {id}");
        
        _logger.LogDebug($"SetAnswer: setRemoteDescription: id={id}: {description.toJSON()}");
        connection.PeerConnection.setRemoteDescription(description);

        _logger.LogDebug($"SetAnswer: wait ice candidates complete: id={id}");
        await connection.IceCollectCompleteTcs.Task.WaitAsync(cancellationToken);

        // //return one candidate
        // var candidate = connection.IceCandidates[0];
        // var candidateJson = candidate.toJSON();
        // return candidateJson;

        //return all candidates
        var candidatesListJson = connection.IceCandidates
            .Select(candidate => candidate.toJSON())
            .ToArray()
            .ToJson();
        _logger.LogDebug($"SetAnswer: result ice candidates: id={id}: {candidatesListJson}");
        return candidatesListJson;
    }
}