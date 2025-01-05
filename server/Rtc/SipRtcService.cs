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

    private class Link(RTCPeerConnection peerConnection)
    {
        public readonly RTCPeerConnection PeerConnection = peerConnection;
        public readonly List<RTCIceCandidate> IceCandidates = [];
        public readonly TaskCompletionSource<List<RTCIceCandidate>> IceCollectCompleteTcs = new();
        public RTCDataChannel? DataChannel;
    }

    private readonly ConcurrentDictionary<string, Link> _link = new();

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
            throw new InvalidOperationException($"Body must contain SDP answer for id: {id}");
        return SetAnswer(id, answer, cancellationToken);
    }

    public ValueTask AddIceCandidates(string id, string candidatesJson, CancellationToken cancellationToken)
    {
        var candidates = candidatesJson.FromJson<RTCIceCandidateInit[]>();
        return AddIceCandidates(id, cancellationToken, candidates);
    }

    private async Task<RTCSessionDescriptionInit> GetOffer(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentNullException(nameof(id), "ID must be supplied to create new peer connection");
        if (_link.ContainsKey(id))
            throw new ArgumentNullException(nameof(id), "ID is already in use");

        _logger.LogDebug($"creating RTCPeerConnection and RTCDataChannel for id={id}");
        var config = new RTCConfiguration
        {
            //iceServers = [new() { urls = "stun:stun.sipsorcery.com" }]
            //iceServers = [new() { urls = "stun:stun.cloudflare.com:3478" }]
            //iceServers = [new() { urls = "stun:stun.l.google.com:19302" }]
            iceServers = [new() { urls = "stun:stun.l.google.com:3478" }]
        };
        var peerConnection = new RTCPeerConnection(config
            // , bindPort: 50100
            // , portRange: new(50100, 50200)
        );
        //var peerConnection = new RTCPeerConnection();

        var link = new Link(peerConnection)
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
            // if (candidate.type == RTCIceCandidateType.host)
            // {
            //     _logger.LogDebug("onicecandidate: skip host candidate");
            //     return;
            // }
            link.IceCandidates.Add(candidate);
        };
        peerConnection.onicecandidateerror += (candidate, error) =>
            _logger.LogWarning($"onicecandidateerror: '{error}' {candidate}");
        peerConnection.oniceconnectionstatechange += state =>
            _logger.LogDebug($"oniceconnectionstatechange: {state}");
        peerConnection.onicegatheringstatechange += state =>
        {
            _logger.LogDebug($"onicegatheringstatechange: {state}");
            if (state == RTCIceGatheringState.complete)
                link.IceCollectCompleteTcs.SetResult(link.IceCandidates);
        };

        peerConnection.OnRtpPacketReceived += (IPEndPoint rep, SDPMediaTypesEnum media, RTPPacket rtpPkt)
            => _logger.LogDebug(
                $"OnRtpPacketReceived: RTP {media} pkt received, SSRC {rtpPkt.Header.SyncSource}, SeqNum {rtpPkt.Header.SequenceNumber}");
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
                _link.TryRemove(id, out _);
            else if (state == RTCPeerConnectionState.connected)
                _logger.LogDebug("onconnectionstatechange: Peer connection connected");
        };

        var channel = link.DataChannel;
        channel.onopen += () =>
        {
            _logger.LogDebug($"DataChannel: onopen: label={channel.label}");

            var frameId = 0;
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

        _link.TryAdd(id, link);

        _logger.LogDebug($"returning offer for id={id}: {offerSdp.toJSON()}");
        return offerSdp;
    }

    private async ValueTask<string> SetAnswer(string id, RTCSessionDescriptionInit description,
        CancellationToken cancellationToken)
    {
        if (!_link.TryGetValue(id, out var link))
            throw new InvalidOperationException($"SetAnswer: peer id not found: {id}");

        _logger.LogDebug($"SetAnswer: setRemoteDescription: id={id}: {description.toJSON()}");
        link.PeerConnection.setRemoteDescription(description);

        _logger.LogDebug($"SetAnswer: wait ice candidates gathering complete: id={id}");
        var candidates = await link.IceCollectCompleteTcs.Task.WaitAsync(cancellationToken);
        var candidatesListJson = candidates
            .Select(candidate => candidate.toJSON())
            .ToArray()
            .ToJson();
        _logger.LogDebug($"SetAnswer: return ice candidates ({candidates.Count} count): id={id}: {candidatesListJson}");
        return candidatesListJson;
    }

    private ValueTask AddIceCandidates(string id, CancellationToken cancellationToken,
        params RTCIceCandidateInit[] candidates)
    {
        if (!_link.TryGetValue(id, out var link))
            throw new InvalidOperationException($"AddIceCandidates: peer id not found: {id}");

        _logger.LogDebug($"AddIceCandidates: id={id}: adding {candidates.Length} candidates");
        foreach (var candidate in candidates)
        {
            _logger.LogDebug($"AddIceCandidates: id={id}: {candidate.toJSON()}");
            link.PeerConnection.addIceCandidate(candidate);
            cancellationToken.ThrowIfCancellationRequested();
        }
        return default;
    }
}