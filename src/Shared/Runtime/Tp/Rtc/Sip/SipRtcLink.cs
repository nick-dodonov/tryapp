#if !UNITY_5_6_OR_NEWER
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Shared.Log;
using SIPSorcery.Net;
using SIPSorcery.Sys;

namespace Shared.Tp.Rtc.Sip
{
    internal class SipRtcLink : ITpLink
    {
        private readonly ILogger _logger;

        internal ITpReceiver? Receiver { get; set; }

        public int LinkId { get; }
        private readonly string _linkToken;

        private readonly SipRtcService _service;
        
        private RTCPeerConnection? _peerConnection;
        private RTCDataChannel? _dataChannel;

        private readonly TaskCompletionSource<List<RTCIceCandidate>> _iceCollectCompleteTcs = new();

        public SipRtcLink(
            int linkId,
            string linkToken,
            SipRtcService service,
            ILoggerFactory loggerFactory)
        {
            LinkId = linkId;
            _linkToken = linkToken;
            
            _service = service;
            _logger = new SipLinkLogger(loggerFactory.CreateLogger<SipRtcLink>(), linkId.ToString());
            _logger.Info(".");
        }

        public async Task<RTCSessionDescriptionInit> Init(RTCConfiguration configuration, PortRange portRange)
        {
            _logger.Info(".");
            
            _peerConnection = new(configuration
                //, bindPort: 40000
                , portRange: portRange
            );

            _dataChannel = await _peerConnection.createDataChannel("test", new()
            {
                ordered = false,
                maxRetransmits = 0
            });

            List<RTCIceCandidate> iceCandidates = new();
            _peerConnection.onicecandidate += candidate =>
            {
                _logger.Info($"onicecandidate: {candidate}");
                iceCandidates.Add(candidate);
            };
            _peerConnection.onicecandidateerror += (candidate, error) =>
                _logger.Warn($"onicecandidateerror: '{error}' {candidate}");
            _peerConnection.oniceconnectionstatechange += state => 
                _logger.Info($"oniceconnectionstatechange: {state}");
            _peerConnection.onicegatheringstatechange += state =>
            {
                _logger.Info($"onicegatheringstatechange: {state}");
                if (state == RTCIceGatheringState.complete)
                    _iceCollectCompleteTcs.SetResult(iceCandidates);
            };

            _peerConnection.onconnectionstatechange += state =>
            {
                _logger.Info($"onconnectionstatechange: {state}");
                if (state is
                    RTCPeerConnectionState.closed or
                    RTCPeerConnectionState.disconnected or
                    RTCPeerConnectionState.failed)
                {
                    //TODO: replace with just notification to dispose outside
                    ((IDisposable)this).Dispose();
                }
            };

            var channel = _dataChannel;
            channel.onopen += () =>
            {
                _logger.Info($"DataChannel: onopen: label={channel.label}");
                _service.StartLinkLogic(this);
            };
            channel.onmessage += (_, _, data) =>
            {
                // //TODO: with diagnostics flags
                // var str = Encoding.UTF8.GetString(data);
                // _logger.Info($"DataChannel: onmessage: {str}");

                Receiver?.Received(this, data);
            };
            channel.onclose += () =>
            {
                _logger.Info($"DataChannel: onclose: label={channel.label}");
                Receiver?.Received(this, null);
            };
            channel.onerror += error => 
                _logger.Error($"DataChannel: onerror: {error}");
            
            _logger.Info("creating offer");
            var offer = _peerConnection.createOffer();

            _logger.Info("setup local description");
            await _peerConnection.setLocalDescription(offer);

            _logger.Info($"result: {offer.toJSON()}");
            return offer;
        }

        private void Close(string reason)
        {
            _logger.Info(reason);
            _iceCollectCompleteTcs.TrySetCanceled();

            _dataChannel?.close();
            _dataChannel = null;

            _peerConnection?.close();
            _peerConnection = null;
        }

        void IDisposable.Dispose()
        {
            Close("dispose");
            _service.RemoveLink(_linkToken);
        }

        public async ValueTask<List<RTCIceCandidate>> SetAnswer(RTCSessionDescriptionInit description, CancellationToken cancellationToken)
        {
            _logger.Info($"setRemoteDescription: {description.toJSON()}");
            if (_peerConnection == null)
                throw new InvalidOperationException("SetAnswer: peer connection not initialized");
            
            _peerConnection.setRemoteDescription(description);
            
            _logger.Info("waiting ice candidates");
            var candidates = await _iceCollectCompleteTcs.Task.WaitAsync(cancellationToken);

            _logger.Info($"result [{candidates.Count}] candidates");
            return candidates;
        }

        public ValueTask AddIceCandidates(RTCIceCandidateInit[] candidates, CancellationToken cancellationToken)
        {
            _logger.Info($"adding [{candidates.Length}] candidates");
            if (_peerConnection == null)
                throw new InvalidOperationException("SetAnswer: peer connection not initialized");

            foreach (var candidate in candidates)
            {
                _logger.Info(candidate.toJSON());
                _peerConnection.addIceCandidate(candidate);
                cancellationToken.ThrowIfCancellationRequested();
            }

            return default;
        }

        string ITpLink.GetRemotePeerId() => _linkToken; //TODO: think maybe _linkId is better for it

        void ITpLink.Send(byte[] bytes)
        {
            if (_dataChannel?.readyState != RTCDataChannelState.open)
            {
                _logger.Warn($"skip: readyState={_dataChannel?.readyState}");
                return;
            }

            var connectionState = _peerConnection?.connectionState;
            if (connectionState != RTCPeerConnectionState.connected)
            {
                _logger.Info($"skip: connectionState={connectionState}");
                return;
            }

            var sctpState = _peerConnection?.sctp.state;
            if (sctpState != RTCSctpTransportState.Connected)
            {
                _logger.Info($"skip: sctp.state={sctpState}");
                return;
            }

            // //TODO: with diagnostics flags
            // var content = Encoding.UTF8.GetString(bytes);
            // _logger.Info($"[{bytes.Length}]: {content}");

            _dataChannel?.send(bytes);
        }
    }
}
#endif