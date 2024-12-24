using System.Collections.Concurrent;
using System.Net;
using SIPSorcery.Net;

namespace Server.Rtc;

/// <summary>
/// Based on several samples
///     examples/WebRTCExamples/WebRTCAspNet
///     examples/WebRTCExamples/WebRTCGetStartedDataChannel
///     https://www.marksort.com/udp-like-networking-in-the-browser/
/// </summary>
public class RtcService : IHostedService
{
    private readonly ILogger<RtcService> _logger;

    private class Connection(RTCPeerConnection peerConnection)
    {
        public readonly RTCPeerConnection PeerConnection = peerConnection;
        public readonly List<RTCIceCandidate> IceCandidates = [];
        public RTCDataChannel DataChannel;
    }
    private readonly ConcurrentDictionary<string, Connection> _connections = new();

    /// <summary>
    /// Based on several samples
    ///     examples/WebRTCExamples/WebRTCAspNet
    ///     examples/WebRTCExamples/WebRTCGetStartedDataChannel
    ///     https://www.marksort.com/udp-like-networking-in-the-browser/
    /// </summary>
    public RtcService(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<RtcService>();
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

    public async Task<RTCSessionDescriptionInit> GetOffer(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentNullException(nameof(id), "ID must be supplied to create new peer connection");
        if (_connections.ContainsKey(id))
            throw new ArgumentNullException(nameof(id), "ID is already in use");

        _logger.LogDebug($"creating RTCPeerConnection for id={id}");
        // const string STUN_URL = "stun:stun.sipsorcery.com";
        // var config = new RTCConfiguration {
        //     iceServers = [new() { urls = STUN_URL }]
        // };
        //var peerConnection = new RTCPeerConnection(config);
        var peerConnection = new RTCPeerConnection();
        var connection = new Connection(peerConnection);

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
            _logger.LogDebug($"onicegatheringstatechange: {state}");
        
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
        
        // peerConnection.ondatachannel += rdc =>
        // {
        //     rdc.onopen += () => logger.LogDebug($"Data channel: onopen: label={rdc.label}");
        //     rdc.onclose += () => logger.LogDebug($"Data channel: onclose: label={rdc.label}");
        //     rdc.onmessage += (datachannel, type, data) =>
        //     {
        //         logger.LogDebug($"Data channel: message: label={rdc.label} type={type}");
        //         rdc.send("echo: 22222222222222222222222222");
        //     };
        // };
        
        _logger.LogDebug($"creating data channel for id={id}");
        var channel = connection.DataChannel = await peerConnection.createDataChannel("test", new()
        {
            ordered = false,
            maxRetransmits = 0
        });
        channel.onopen += () =>
        {
            _logger.LogDebug($"DataChannel: onopen: label={channel.label}");
            
            var frameId = 1;
            var timer = new System.Timers.Timer(1000); // Timer interval set to 1 second
            timer.Elapsed += (sender, e) =>
            {
                if (peerConnection.connectionState != RTCPeerConnectionState.connected)
                {
                    _logger.LogDebug("DataChannel: Peer connection is connected, stopping frame ID timer.");
                    timer.Stop();
                    return;
                }

                var frameIdToSend = $"{frameId++};TODO-RANDOM-DATA";
                _logger.LogDebug($"Sending frame ID: {frameIdToSend}");
                channel.send(frameIdToSend);
            };
            timer.Start();
        };
        channel.onclose += () => _logger.LogDebug($"DataChannel: onclose: label={channel.label}");
        channel.onmessage += (datachannel, type, data) =>
            _logger.LogDebug($"DataChannel: onmessage: type={type} data=[{data.Length}]");
        channel.onerror += error => _logger.LogError($"DataChannel: error: {error}");
        
        _logger.LogDebug($"creating offer for id={id}");
        var offerSdp = peerConnection.createOffer();
        await peerConnection.setLocalDescription(offerSdp);

        _connections.TryAdd(id, connection);

        _logger.LogDebug($"returning offer for id={id}: {offerSdp}");
        return offerSdp;
    }
    
    public async ValueTask<string> SetAnswer(string id, RTCSessionDescriptionInit description, CancellationToken cancellationToken)
    {
        if (!_connections.TryGetValue(id, out var connection))
            throw new ApplicationException($"No peer connection is available for the specified id: {id}");
        
        _logger.LogDebug($"SetAnswer for id={id}: {description.toJSON()}");
        connection.PeerConnection.setRemoteDescription(description);

        //TODO: wait at least one is ready
        await Task.Yield();
        
        //TODO: answer all candidates
        var candidate = connection.IceCandidates[0];
        var result = candidate.toJSON();
        return result;
    }
    
    public void TestSend(string id)
    {
        if (!_connections.TryGetValue(id, out var pc))
            throw new ApplicationException($"No peer connection is available for the specified id: {id}");
        var channel = pc.DataChannel;
        
        var random = new Random();
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var randomString = new string(Enumerable.Repeat(chars, 16).Select(s => s[random.Next(s.Length)]).ToArray());

        _logger.LogDebug($"TestSend: readyState={channel.readyState} randomString={randomString}");
        channel.send(randomString);
    }
}