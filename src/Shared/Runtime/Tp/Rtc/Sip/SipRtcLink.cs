#if !UNITY_5_6_OR_NEWER
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Shared.Log;
using Shared.Log.Asp;
using SIPSorcery.Net;
using SIPSorcery.Sys;

namespace Shared.Tp.Rtc.Sip
{
    internal sealed class SipRtcLink : ITpLink
    {
        private readonly ILogger _logger;

        public int LinkId { get; }
        private readonly string _linkToken;
        private readonly string _remotePeerId;

        private readonly SipRtcService _service;

        private RTCPeerConnection? _peerConnection;
        private RTCDataChannel? _dataChannel;

        private readonly TaskCompletionSource<List<RTCIceCandidate>> _iceCollectCompleteTcs = new();

        private ITpReceiver? _receiver;
        private PostponedBytes _receivePostponed; 

        public SipRtcLink(
            int linkId,
            string linkToken,
            SipRtcService service,
            ILoggerFactory loggerFactory)
        {
            LinkId = linkId;
            _linkToken = linkToken;

            //decided to use linkId as remote peer identification
            //TODO: add some hash (but NOT full token as it always personal for link)
            _remotePeerId = linkId.ToString();

            _service = service;
            _logger = new IdLogger(loggerFactory.CreateLogger<SipRtcLink>(), _remotePeerId);
            _logger.Info(".");
        }

        public override string ToString() => $"{nameof(SipRtcLink)}<{_remotePeerId}>"; //only for diagnostics

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
                _logger.Info($"onicecandidate: {candidate.toJSON()}");
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
                    Close(state.ToString());
                }
            };

            var channel = _dataChannel;
            channel.onopen += () =>
            {
                _logger.Info($"DataChannel: onopen: label={channel.label}");
                CallConnected();
            };
            channel.onmessage += (_, _, data) =>
            {
                // TODO: with diagnostics flags
                // var str = Encoding.UTF8.GetString(data);
                // _logger.Info($"DataChannel: onmessage: {str}");
                CallReceived(data);
            };
            channel.onclose += () =>
            {
                _logger.Info($"DataChannel: onclose: label={channel.label}");
                CallDisconnected();
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
            
            _service.RemoveLink(_linkToken);
        }

        void IDisposable.Dispose() => Close("disposing");

        public async ValueTask<List<RTCIceCandidate>> SetAnswer(RTCSessionDescriptionInit description,
            CancellationToken cancellationToken)
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

        string ITpLink.GetRemotePeerId() => _remotePeerId;

        public void Send(ReadOnlySpan<byte> span)
        {
            if (_dataChannel?.readyState != RTCDataChannelState.open)
            {
                _logger.Warn($"skip: readyState={_dataChannel?.readyState}");
                return;
            }

            var connectionState = _peerConnection?.connectionState;
            if (connectionState != RTCPeerConnectionState.connected)
            {
                _logger.Warn($"skip: connectionState={connectionState}");
                return;
            }

            var sctpState = _peerConnection?.sctp.state;
            if (sctpState != RTCSctpTransportState.Connected)
            {
                _logger.Warn($"skip: sctp.state={sctpState}");
                return;
            }

            // //TODO: with diagnostics flags
            // var content = Encoding.UTF8.GetString(bytes);
            // _logger.Info($"[{bytes.Length}]: {content}");

            //TODO: speedup: ask/modify SIPSorcery to support ReadOnlySpan 
            var bytes = span.ToArray();
            _dataChannel?.send(bytes);
        }

        void ITpLink.Send<T>(TpWriteCb<T> writeCb, in T state)
        {
            try
            {
                var writer = new ArrayBufferWriter<byte>(); //TODO: speedup: use pooled / cached writer
                writeCb(writer, state);
                Send(writer.WrittenSpan);
            }
            catch (Exception e)
            {
                _logger.Error($"failed: {e}");
            }
        }

        private void CallConnected()
        {
            try
            {
                _receiver = _service.CallConnected(this);
                if (_receiver == null)
                {
                    Close("not listened");
                    return;
                }
                _receivePostponed.Feed(static (link, bytes) =>
                {
                    if (bytes != null)
                        link._receiver!.Received(link, bytes);
                    else
                        link._receiver!.Disconnected(link);
                }, this);
            }
            catch (Exception e)
            {
                _logger.Error($"listener failed: {e}");
                Close("listener failed");
            }
        }
        
        private void CallReceived(byte[] bytes)
        {
            if (_receiver != null)
                _receiver.Received(this, bytes);
            else
            {
                _logger.Warn($"no receiver, postpone [{bytes.Length}] bytes");
                _receivePostponed.Add(bytes);
            }
        }
        
        private void CallDisconnected()
        {
            if (_receiver != null)
                _receiver.Disconnected(this);
            else
            {
                _logger.Warn("no receiver, postpone disconnected");
                _receivePostponed.Disconnect();
            }
        }
    }
}
#endif