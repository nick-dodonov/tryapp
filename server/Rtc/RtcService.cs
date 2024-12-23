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
public class RtcService(ILogger<RtcService> logger) : IHostedService
{
    private record Connection(RTCPeerConnection PeerConnection, RTCDataChannel DataChannel);
    private readonly ConcurrentDictionary<string, Connection> _peerConnections = new();

    public Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogDebug("StartAsync");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogDebug("StopAsync");
        return Task.CompletedTask;
    }

    private const string STUN_URL = "stun:stun.sipsorcery.com";
    public async Task<RTCSessionDescriptionInit> GetOffer(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentNullException(nameof(id), "ID must be supplied to create new peer connection");
        if (_peerConnections.ContainsKey(id))
            throw new ArgumentNullException(nameof(id), "ID is already in use");
        
        // var config = new RTCConfiguration {
        //     iceServers = [new() { urls = STUN_URL }]
        // };
        //var peerConnection = new RTCPeerConnection(config);
        var peerConnection = new RTCPeerConnection();
        
        peerConnection.OnRtpPacketReceived += (IPEndPoint rep, SDPMediaTypesEnum media, RTPPacket rtpPkt) 
            => logger.LogDebug($"OnRtpPacketReceived: RTP {media} pkt received, SSRC {rtpPkt.Header.SyncSource}, SeqNum {rtpPkt.Header.SequenceNumber}");
        peerConnection.OnReceiveReport += (IPEndPoint ip, SDPMediaTypesEnum media, RTCPCompoundPacket pkt) 
            => logger.LogDebug($"OnReceiveReport: RTP {media}");
        peerConnection.OnSendReport += (SDPMediaTypesEnum media, RTCPCompoundPacket pkt) 
            => logger.LogDebug($"OnSendReport: RTP {media}");
        peerConnection.OnTimeout += media 
            => logger.LogWarning($"OnTimeout: {media}");
        peerConnection.onconnectionstatechange += state =>
        {
            logger.LogDebug($"onconnectionstatechange: Peer {id} state changed to {state}");
            if (state is 
                RTCPeerConnectionState.closed or 
                RTCPeerConnectionState.disconnected or 
                RTCPeerConnectionState.failed)
                _peerConnections.TryRemove(id, out _);
            else if (state == RTCPeerConnectionState.connected)
                logger.LogDebug("onconnectionstatechange: Peer connection connected");
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
        var dataChannel = await peerConnection.createDataChannel("test", new()
        {
            ordered = false,
            maxRetransmits = 0
        });
        
        var offerSdp = peerConnection.createOffer();
        await peerConnection.setLocalDescription(offerSdp);

        _peerConnections.TryAdd(id, new(peerConnection, dataChannel));
        
        peerConnection.onicecandidate += candidate => 
            logger.LogDebug($"onicecandidate: {candidate}");
        peerConnection.onicecandidateerror += (candidate, error) =>
            logger.LogWarning($"onicecandidateerror: '{error}' {candidate}");
        peerConnection.oniceconnectionstatechange += state => 
            logger.LogDebug($"oniceconnectionstatechange: {state}");
        peerConnection.onicegatheringstatechange += state =>
            logger.LogDebug($"onicegatheringstatechange: {state}");
            
        return offerSdp;
    }
    
    public void SetRemoteDescription(string id, RTCSessionDescriptionInit description)
    {
        if (!_peerConnections.TryGetValue(id, out var pc))
            throw new ApplicationException($"No peer connection is available for the specified id: {id}");
        
        logger.LogDebug($"SetRemoteDescription: answer: {description.toJSON()}");
        pc.PeerConnection.setRemoteDescription(description);
    }

    public void TestSend(string id)
    {
        if (!_peerConnections.TryGetValue(id, out var pc))
            throw new ApplicationException($"No peer connection is available for the specified id: {id}");
        var channel = pc.DataChannel;
        
        var random = new Random();
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var randomString = new string(Enumerable.Repeat(chars, 16).Select(s => s[random.Next(s.Length)]).ToArray());

        logger.LogDebug($"TestSend: readyState={channel.readyState} randomString={randomString}");
        channel.send(randomString);
    }
}