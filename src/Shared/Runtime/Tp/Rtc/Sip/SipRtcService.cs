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
using TinyJson;

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

        //TODO: replace ID with number because now it's not specific client ID but just token 
        private readonly ConcurrentDictionary<string, SipRtcLink> _links = new();

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
            var id = Interlocked.Increment(ref _globalLinkCounter).ToString();

            _logger.Info($"creating new link for id={id}");
            var link = new SipRtcLink(id, this, _loggerFactory);
            _links.TryAdd(id, link);

            //TODO: mv RTCConfiguration to .ctr and appsettings.json
            var configuration = new RTCConfiguration
            {
                //iceServers = [new() { urls = "stun:stun.sipsorcery.com" }]
                //iceServers = [new() { urls = "stun:stun.cloudflare.com:3478" }]
                //iceServers = [new() { urls = "stun:stun.l.google.com:19302" }]
                //iceServers = [new() { urls = "stun:stun.l.google.com:3478" }]
            };
            var sdpInit = await link.Init(configuration, _portRange);

            //TODO: current http-signal protocol part will be removed when shared RTC structs will appear
            return new()
            {
                LinkId = id,
                SdpInitJson = sdpInit.toJSON()
            };
        }

        async ValueTask<string> IRtcService.SetAnswer(string id, string answerJson, CancellationToken cancellationToken)
        {
            //TODO: current http-signal protocol part will be removed when shared RTC structs will appear
            if (!RTCSessionDescriptionInit.TryParse(answerJson, out var answer))
                throw new InvalidOperationException($"SetAnswer: body must contain SDP answer for id: {id}");

            if (!_links.TryGetValue(id, out var link))
                throw new InvalidOperationException($"SetAnswer: peer id not found: {id}");

            var candidates = await link.SetAnswer(answer, cancellationToken);

            //TODO: current http-signal protocol part will be removed when shared RTC structs will appear
            var candidatesListJson = candidates
                .Select(candidate => candidate.toJSON())
                .ToArray()
                .ToJson();
            _logger.Info($"result for id={id}: {candidatesListJson}");
            return candidatesListJson;
        }

        ValueTask IRtcService.AddIceCandidates(string id, string candidatesJson, CancellationToken cancellationToken)
        {
            //TODO: current http-signal protocol part will be removed when shared RTC structs will appear
            var candidates = candidatesJson.FromJson<RTCIceCandidateInit[]>();
            if (!_links.TryGetValue(id, out var link))
                throw new InvalidOperationException($"AddIceCandidates: peer id not found: {id}");

            return link.AddIceCandidates(candidates, cancellationToken);
        }

        internal void RemoveLink(string id) => _links.TryRemove(id, out _);

        internal void StartLinkLogic(SipRtcLink link)
        {
            var receiver = _listener?.Connected(link);
            link.Receiver = receiver;
        }

        Task<ITpLink> ITpApi.Connect(ITpReceiver receiver, CancellationToken cancellationToken) 
            => throw new NotSupportedException("Connect: server side doesn't connect now");

        void ITpApi.Listen(ITpListener listener) => _listener = listener;
    }
}
#endif