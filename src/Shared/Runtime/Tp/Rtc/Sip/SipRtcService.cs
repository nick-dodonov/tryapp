#if !UNITY_5_6_OR_NEWER
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shared.Log;
using SIPSorcery.Net;
using SIPSorcery.Sys;
using TinyJson;

namespace Shared.Tp.Rtc.Sip
{
    /// <summary>
    /// Based on several samples
    ///     examples/WebRTCExamples/WebRTCAspNet
    ///     examples/WebRTCExamples/WebRTCGetStartedDataChannel
    ///     https://www.marksort.com/udp-like-networking-in-the-browser/
    /// </summary>
    public class SipRtcService : IHostedService, IRtcService, ITpApi
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
            _logger.Info(".");
            return Task.CompletedTask;
        }

        Task IHostedService.StopAsync(CancellationToken cancellationToken)
        {
            _logger.Info(".");
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

            _logger.Info($"creating RTCPeerConnection and RTCDataChannel for id={id}");
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

            _logger.Info($"creating offer for id={id}");
            var offerSdp = peerConnection.createOffer();
            await peerConnection.setLocalDescription(offerSdp);

            _links.TryAdd(id, link);

            _logger.Info($"returning offer for id={id}: {offerSdp.toJSON()}");
            return offerSdp;
        }

        private async ValueTask<string> SetAnswer(string id, 
            RTCSessionDescriptionInit description,
            CancellationToken cancellationToken)
        {
            if (!_links.TryGetValue(id, out var link))
                throw new InvalidOperationException($"SetAnswer: peer id not found: {id}");

            var candidates = await link.SetAnswer(description, cancellationToken);

            var candidatesListJson = candidates
                .Select(candidate => candidate.toJSON())
                .ToArray()
                .ToJson();
            _logger.Info($"result for id={id}: {candidatesListJson}");
            return candidatesListJson;
        }

        private ValueTask AddIceCandidates(string id, CancellationToken cancellationToken, params RTCIceCandidateInit[] candidates)
        {
            if (!_links.TryGetValue(id, out var link))
                throw new InvalidOperationException($"AddIceCandidates: peer id not found: {id}");

            _logger.Info($"id={id}: adding {candidates.Length} candidates");
            foreach (var candidate in candidates)
            {
                _logger.Info($"id={id}: {candidate.toJSON()}");
                link.PeerConnection.addIceCandidate(candidate);
                cancellationToken.ThrowIfCancellationRequested();
            }
            return default;
        }

        internal void RemoveLink(string id) => _links.TryRemove(id, out _);

        internal void StartLinkLogic(SipRtcLink link)
        {
            var receiver = _listener?.Connected(link);
            link.Receiver = receiver;
        }
    
        Task<ITpLink> ITpApi.Connect(ITpReceiver receiver, CancellationToken cancellationToken) 
            => throw new NotSupportedException();

        private ITpListener? _listener;
        void ITpApi.Listen(ITpListener listener) => _listener = listener;
    }
}
#endif