using System.Collections.Concurrent;
using Shared.Rtc;
using Shared.Session;
using Shared.Web;
using SIPSorcery.Net;
using SIPSorcery.Sys;
using TinyJson;

namespace Server.Rtc;

/// <summary>
/// Based on several samples
///     examples/WebRTCExamples/WebRTCAspNet
///     examples/WebRTCExamples/WebRTCGetStartedDataChannel
///     https://www.marksort.com/udp-like-networking-in-the-browser/
/// </summary>
public class SipRtcService : IHostedService, IRtcService, IRtcApi
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<SipRtcService> _logger;

    private readonly ConcurrentDictionary<string, SipRtcLink> _links = new();
    /// <summary>
    /// PortRange must be shared otherwise new RTCPeerConnection() fails on MAXIMUM_UDP_PORT_BIND_ATTEMPTS (25) allocation 
    /// </summary>
    private readonly PortRange _portRange = new(40000, 60000);

    /// <summary>
    /// Based on several samples
    ///     examples/WebRTCExamples/WebRTCAspNet
    ///     examples/WebRTCExamples/WebRTCGetStartedDataChannel
    ///     https://www.marksort.com/udp-like-networking-in-the-browser/
    /// </summary>
    public SipRtcService(ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory;
        _logger = loggerFactory.CreateLogger<SipRtcService>();
        SIPSorcery.LogFactory.Set(loggerFactory);
    }

    Task IHostedService.StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("StartAsync");
        return Task.CompletedTask;
    }

    Task IHostedService.StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("StopAsync");
        return Task.CompletedTask;
    }

    async ValueTask<string> IRtcService.GetOffer(string id, CancellationToken cancellationToken)
        => (await GetOffer(id)).toJSON();

    ValueTask<string> IRtcService.SetAnswer(string id, string answerJson, CancellationToken cancellationToken)
    {
        if (!RTCSessionDescriptionInit.TryParse(answerJson, out var answer))
            throw new InvalidOperationException($"Body must contain SDP answer for id: {id}");
        return SetAnswer(id, answer, cancellationToken);
    }

    ValueTask IRtcService.AddIceCandidates(string id, string candidatesJson, CancellationToken cancellationToken)
    {
        var candidates = candidatesJson.FromJson<RTCIceCandidateInit[]>();
        return AddIceCandidates(id, cancellationToken, candidates);
    }

    private async Task<RTCSessionDescriptionInit> GetOffer(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentNullException(nameof(id), "ID must be supplied to create new peer connection");
        if (_links.ContainsKey(id))
            throw new ArgumentNullException(nameof(id), "ID is already in use");

        _logger.LogDebug($"creating RTCPeerConnection and RTCDataChannel for id={id}");
        var config = new RTCConfiguration
        {
            //iceServers = [new() { urls = "stun:stun.sipsorcery.com" }]
            //iceServers = [new() { urls = "stun:stun.cloudflare.com:3478" }]
            //iceServers = [new() { urls = "stun:stun.l.google.com:19302" }]
            //iceServers = [new() { urls = "stun:stun.l.google.com:3478" }]
        };
        var peerConnection = new RTCPeerConnection(config
            //, bindPort: 40000
            , portRange: _portRange
        );
        var link = new SipRtcLink(this, id, peerConnection, _loggerFactory);
        await link.Init();

        peerConnection.onconnectionstatechange += state =>
        {
            _logger.LogDebug($"onconnectionstatechange: Peer {id} state changed to {state}");
            if (state is
                RTCPeerConnectionState.closed or
                RTCPeerConnectionState.disconnected or
                RTCPeerConnectionState.failed)
            {
                _logger.LogDebug($"onconnectionstatechange: Peer {id} closing");
                if (_links.TryRemove(id, out link) && link != null)
                {
                    link.IceCollectCompleteTcs.TrySetCanceled();
                    link.PeerConnection.close();
                }
            }
            else if (state == RTCPeerConnectionState.connected)
                _logger.LogDebug("onconnectionstatechange: Peer connection connected");
        };

        _logger.LogDebug($"creating offer for id={id}");
        var offerSdp = peerConnection.createOffer();
        await peerConnection.setLocalDescription(offerSdp);

        _links.TryAdd(id, link);

        _logger.LogDebug($"returning offer for id={id}: {offerSdp.toJSON()}");
        return offerSdp;
    }

    private async ValueTask<string> SetAnswer(string id, RTCSessionDescriptionInit description,
        CancellationToken cancellationToken)
    {
        if (!_links.TryGetValue(id, out var link))
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

    private ValueTask AddIceCandidates(string id, CancellationToken cancellationToken, params RTCIceCandidateInit[] candidates)
    {
        if (!_links.TryGetValue(id, out var link))
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

    public void RemoveLink(string id) => _links.TryRemove(id, out _);

    public void StartLinkLogic(RTCDataChannel channel, RTCPeerConnection peerConnection)
    {
        var frameId = 0;
        var timer = new System.Timers.Timer(1000); // Timer interval set to 1 second
        timer.Elapsed += (_, _) =>
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

            var utcMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            //var msg = $"{frameId};TODO-FROM-SERVER;{utcMs}";

            var peerStates = _links.Select(kv => new PeerState
            {
                Id = kv.Key,
                ClientState = kv.Value.LastClientState
            }).ToArray();
            var serverStateMsg = new ServerState
            {
                Frame = frameId,
                UtcMs = utcMs,
                Peers = peerStates
            };
            var msg = WebSerializer.SerializeObject(serverStateMsg);
                
            frameId++;
            _logger.LogDebug($"DataChannel: sending: {msg}");
            channel.send(msg);
        };
        timer.Start();
    }
    
    Task<IRtcLink> IRtcApi.Connect(IRtcLink.ReceivedCallback receivedCallback, CancellationToken cancellationToken) 
        => throw new NotSupportedException();

    private IRtcApi.ConnectionCallback _connectionCallback;
    void IRtcApi.Listen(IRtcApi.ConnectionCallback connectionCallback) 
        => _connectionCallback = connectionCallback;
}