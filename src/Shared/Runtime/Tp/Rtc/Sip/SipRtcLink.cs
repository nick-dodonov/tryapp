#if !UNITY_5_6_OR_NEWER
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Shared.Log;
using SIPSorcery.Net;

namespace Shared.Tp.Rtc.Sip
{
    internal class SipRtcLink : ITpLink
    {
        private readonly ILogger _logger;

        public RTCPeerConnection PeerConnection => _peerConnection;
        internal ITpReceiver? Receiver { get; set; }

        private readonly List<RTCIceCandidate> _iceCandidates = new();
        public readonly TaskCompletionSource<List<RTCIceCandidate>> IceCollectCompleteTcs = new();
        private RTCDataChannel? _dataChannel;
        private readonly SipRtcService _service;
        private readonly string _id;
        private readonly RTCPeerConnection _peerConnection;

        public SipRtcLink(SipRtcService service,
            string id,
            RTCPeerConnection peerConnection,
            ILoggerFactory loggerFactory)
        {
            _service = service;
            _id = id;
            _peerConnection = peerConnection;
            _logger = new SipIdLogger(loggerFactory.CreateLogger<SipRtcLink>(), id);
        }

        public async Task Init()
        {
            _logger.Info(".");
            _dataChannel = await _peerConnection.createDataChannel("test", new()
            {
                ordered = false,
                maxRetransmits = 0
            });

            _peerConnection.onicecandidate += candidate =>
            {
                _logger.Info($"onicecandidate: {candidate}");
                _iceCandidates.Add(candidate);
            };
            _peerConnection.onicecandidateerror += (candidate, error) =>
                _logger.Warn($"onicecandidateerror: '{error}' {candidate}");
            _peerConnection.oniceconnectionstatechange += state => 
                _logger.Info($"oniceconnectionstatechange: {state}");
            _peerConnection.onicegatheringstatechange += state =>
            {
                _logger.Info($"onicegatheringstatechange: {state}");
                if (state == RTCIceGatheringState.complete)
                    IceCollectCompleteTcs.SetResult(_iceCandidates);
            };

            _peerConnection.onconnectionstatechange += state =>
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
                _service.StartLinkLogic(this);
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
            _service.RemoveLink(_id);
            IceCollectCompleteTcs.TrySetCanceled();
            _peerConnection.close();
        }

        string ITpLink.GetRemotePeerId() => _id;

        void ITpLink.Send(byte[] bytes)
        {
            if (_dataChannel?.readyState != RTCDataChannelState.open)
            {
                _logger.Warn($"skip: readyState={_dataChannel?.readyState}");
                return;
            }
            if (_peerConnection.connectionState != RTCPeerConnectionState.connected)
            {
                _logger.Info($"skip: connectionState={_peerConnection.connectionState}");
                return;
            }
            if (_peerConnection.sctp.state != RTCSctpTransportState.Connected)
            {
                _logger.Info($"skip: sctp.state={_peerConnection.sctp.state}");
                return;
            }
        
            var content = Encoding.UTF8.GetString(bytes);
            _logger.Info($"[{bytes.Length}]: {content}");
            _dataChannel?.send(bytes);
        }
    }
}
#endif