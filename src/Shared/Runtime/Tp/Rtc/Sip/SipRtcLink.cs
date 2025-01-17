#if !UNITY_5_6_OR_NEWER
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Shared.Log;
using SIPSorcery.Net;

namespace Shared.Tp.Rtc.Sip
{
    internal class SipRtcLink : ITpLink
    {
        private readonly ILogger _logger;

        internal ITpReceiver? Receiver { get; set; }

        private readonly List<RTCIceCandidate> _iceCandidates = new();
        private readonly TaskCompletionSource<List<RTCIceCandidate>> _iceCollectCompleteTcs = new();
        private RTCDataChannel? _dataChannel;
        
        private readonly SipRtcService _service;
        private readonly string _remotePeerId;
        private readonly RTCPeerConnection _peerConnection;

        private RTCSessionDescriptionInit? _offer;
        
        public SipRtcLink(
            SipRtcService service,
            string remotePeerId,
            RTCPeerConnection peerConnection,
            ILoggerFactory loggerFactory)
        {
            _service = service;
            _remotePeerId = remotePeerId;
            _peerConnection = peerConnection;
            _logger = new SipIdLogger(loggerFactory.CreateLogger<SipRtcLink>(), remotePeerId);
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
                    _iceCollectCompleteTcs.SetResult(_iceCandidates);
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

        public async ValueTask<RTCSessionDescriptionInit> GetOffer()
        {
            if (_offer == null)
            {
                _logger.Info("creating offer");
                _offer = _peerConnection.createOffer();

                _logger.Info("setup local description");
                await _peerConnection.setLocalDescription(_offer);
            }

            _logger.Info($"result: {_offer.toJSON()}");
            return _offer;
        }

        void IDisposable.Dispose()
        {
            _logger.Info(".");
            _service.RemoveLink(_remotePeerId);
            _iceCollectCompleteTcs.TrySetCanceled();
            _peerConnection.close();
        }

        public async ValueTask<List<RTCIceCandidate>> SetAnswer(RTCSessionDescriptionInit description, CancellationToken cancellationToken)
        {
            _logger.Info($"setRemoteDescription: {description.toJSON()}");
            _peerConnection.setRemoteDescription(description);
            
            _logger.Info("waiting ice candidates");
            //TODO: shared WaitAsync to use code like
            //  var candidates = await link.IceCollectCompleteTcs.Task.WaitAsync(cancellationToken);
            var task = _iceCollectCompleteTcs.Task;
            var candidates = await Task.WhenAny(
                task, Task.Delay(Timeout.Infinite, cancellationToken)) == task
                ? task.Result
                : throw new OperationCanceledException(cancellationToken);

            _logger.Info($"result [{candidates.Count}] candidates");
            return candidates;
        }

        public ValueTask AddIceCandidates(RTCIceCandidateInit[] candidates, CancellationToken cancellationToken)
        {
            _logger.Info($"adding [{candidates.Length}] candidates");
            foreach (var candidate in candidates)
            {
                _logger.Info(candidate.toJSON());
                _peerConnection.addIceCandidate(candidate);
                cancellationToken.ThrowIfCancellationRequested();
            }

            return default;
        }

        string ITpLink.GetRemotePeerId() => _remotePeerId;

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