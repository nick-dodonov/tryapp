#if !UNITY_5_6_OR_NEWER
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Shared.Log;
using SIPSorcery.Net;
using SIPSorcery.Sys;

namespace Shared.Tp.Rtc.Sip
{
    /// <summary>
    /// Based on several samples
    ///     examples/WebRTCExamples/WebRTCAspNet
    ///     examples/WebRTCExamples/WebRTCGetStartedDataChannel
    ///     https://www.marksort.com/udp-like-networking-in-the-browser/
    /// </summary>
    public class SipRtcService : IRtcService, ITpApi
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger<SipRtcService> _logger;

        private readonly ConcurrentDictionary<string, SipRtcLink> _links = new(); // token->link

        /// <summary>
        /// PortRange must be shared otherwise new RTCPeerConnection() fails on MAXIMUM_UDP_PORT_BIND_ATTEMPTS (25) allocation 
        /// </summary>
        private readonly PortRange _portRange = new(40000, 60000);

        private int _globalLinkCounter;
        private ITpListener? _listener;

        public SipRtcService(ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;
            _logger = loggerFactory.CreateLogger<SipRtcService>();

            var sipVersion = typeof(RTCPeerConnection).Assembly.GetName().Version;
            _logger.Info($"SIPSorcery version {sipVersion}");
            SIPSorcery.LogFactory.Set(loggerFactory);
        }

        async ValueTask<RtcOffer> IRtcService.GetOffer(CancellationToken cancellationToken)
        {
            var id = Interlocked.Increment(ref _globalLinkCounter);
            var token = $"{id}:{Guid.NewGuid().ToString()}";

            _logger.Info($"creating new link for id={id}");
            var link = new SipRtcLink(id, token, this, _loggerFactory);
            _links.TryAdd(token, link);

            //TODO: mv RTCConfiguration to .ctr and appsettings.json
            var configuration = new RTCConfiguration
            {
                //iceServers = [new() { urls = "stun:stun.sipsorcery.com" }]
                //iceServers = [new() { urls = "stun:stun.cloudflare.com:3478" }]
                //iceServers = [new() { urls = "stun:stun.l.google.com:19302" }]
                //iceServers = [new() { urls = "stun:stun.l.google.com:3478" }]
            };
            var sdpInit = await link.Init(configuration, _portRange);

            return new()
            {
                LinkId = id,
                LinkToken = token,
                SdpInit = sdpInit.ToShared()
            };
        }

        async ValueTask<RtcIcInit[]> IRtcService.SetAnswer(string token, RtcSdpInit answer, CancellationToken cancellationToken)
        {
            if (!_links.TryGetValue(token, out var link))
                throw new InvalidOperationException($"SetAnswer: link not found for token: {token}");

            var sipAnswer = answer.FromShared();
            var sipCandidates = await link.SetAnswer(sipAnswer, cancellationToken);
            var candidates = sipCandidates
                .Select(x => x.ToShared())
                .ToArray();

            _logger.Info($"result for id={link.LinkId}: [{candidates.Length}] candidates");
            return candidates;
        }

        ValueTask IRtcService.AddIceCandidates(string token, RtcIcInit[] candidates, CancellationToken cancellationToken)
        {
            if (!_links.TryGetValue(token, out var link))
                throw new InvalidOperationException($"AddIceCandidates: link not found for token: {token}");

            var sipCandidates = candidates
                .Select(x => x.FromShared())
                .ToArray();
            
            return link.AddIceCandidates(sipCandidates, cancellationToken);
        }

        internal void RemoveLink(string token) => _links.TryRemove(token, out _);
        internal ValueTask<ITpReceiver?> ListenerConnected(SipRtcLink link) => 
            _listener?.Connected(link) ?? default;

        ValueTask<ITpLink> ITpApi.Connect(ITpReceiver receiver, CancellationToken cancellationToken) 
            => throw new NotSupportedException("Connect: server side doesn't connect now");

        void ITpApi.Listen(ITpListener listener) => _listener = listener;
    }
}
#endif