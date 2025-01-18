#if UNITY_5_6_OR_NEWER && (UNITY_EDITOR || !UNITY_WEBGL)
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Shared.Web;
using Unity.WebRTC;

namespace Shared.Tp.Rtc.Unity
{
    public class UnityRtcLink : BaseRtcLink
    {
        private RTCPeerConnection? _peerConnection;
        private RTCDataChannel? _dataChannel;
        private readonly List<RTCIceCandidateInit> _iceCandidates = new();
        
        private readonly TaskCompletionSource<RTCDataChannel> _dataChannelTcs = new();

        public UnityRtcLink(IRtcService service, ITpReceiver receiver)
            : base(service, receiver)
        {}

        public async Task Connect(CancellationToken cancellationToken)
        {
            var sharedSdp = await ObtainOffer(cancellationToken);
            var offer = sharedSdp.FromShared();
            _log.Info($"request: {UnityRtcDebug.Describe(offer)}");
            
            _peerConnection = new();
            _peerConnection.OnIceCandidate = candidate =>
            {
                _log.Info($"OnIceCandidate: {UnityRtcDebug.Describe(candidate)}");
                _iceCandidates.Add(new()
                {
                    candidate = candidate.Candidate,
                    sdpMid = candidate.SdpMid,
                    sdpMLineIndex = candidate.SdpMLineIndex
                });
            };
            // ReSharper disable once AsyncVoidLambda
            _peerConnection.OnIceGatheringStateChange = async state =>
            {
                try
                {
                    _log.Info($"OnIceGatheringStateChange: {state}");
                    if (state == RTCIceGatheringState.Complete)
                    {
                        var candidates = _iceCandidates
                            .Select(x => x.ToShared())
                            .ToArray();
                        await ReportIceCandidates(candidates, cancellationToken);
                    }
                }
                catch (Exception ex)
                {
                    _log.Error($"OnIceGatheringStateChange: ReportIceCandidates: failed: {ex}");
                }
            };
            _peerConnection.OnIceConnectionChange = state => 
                _log.Info($"OnIceConnectionChange: {state}");
            _peerConnection.OnConnectionStateChange = state =>
            {
                _log.Info($"OnConnectionStateChange: {state}");
                if (_dataChannel == null)
                    return;
                switch (state)
                {
                    case RTCPeerConnectionState.Disconnected:
                    case RTCPeerConnectionState.Failed:
                    case RTCPeerConnectionState.Closed:
                        CallReceived(null);
                        _dataChannel = null;
                        break;
                }
            };
            _peerConnection.OnDataChannel = channel =>
            {
                _log.Info($"OnDataChannel: {UnityRtcDebug.Describe(channel)}");
                _dataChannel = channel;
                _dataChannelTcs.TrySetResult(channel);
                channel.OnMessage = CallReceived;
                channel.OnOpen = () => _log.Info($"OnDataChannel: OnOpen: {channel}");
                channel.OnClose = () => _log.Info($"OnDataChannel: OnClose: {channel}");
                channel.OnError = error => _log.Info($"OnDataChannel: OnError: {error}");
            };

            await _peerConnection.SetRemoteDescription(ref offer);
            _log.Info("creating answer");
            var answerOp = _peerConnection.CreateAnswer();
            await answerOp;
            var answer = answerOp.Desc;
            _log.Info($"created answer: {UnityRtcDebug.Describe(answer)}");
            await _peerConnection.SetLocalDescription(ref answer);
            
            // send answer to remote side and obtain remote ice candidates
            var candidates = await ReportAnswer(answer.ToShared(), cancellationToken);
            
            // add remote ICE candidates
            foreach (var candidate in candidates)
            {
                //Slog.Info($"UnityRtcLink: AddIceCandidate: json: {candidateJson}");
                var unityCandidateInit = candidate.FromShared();
                //Slog.Info($"UnityRtcLink: AddIceCandidate: init: {UnityRtcDebug.Describe(candidateInit)}");
                var unityCandidate = new RTCIceCandidate(unityCandidateInit);
                _log.Info($"AddIceCandidate: {UnityRtcDebug.Describe(unityCandidate)}");
                var rc = _peerConnection.AddIceCandidate(unityCandidate);
                if (!rc) 
                    _log.Error("AddIceCandidate: FAILED");
            }
            
            _log.Info("Awaiting data channel");
            await _dataChannelTcs.Task.WaitAsync(cancellationToken);
        }

        public override void Dispose()
        {
            _log.Info("Dispose");
            _dataChannelTcs.TrySetCanceled();
            _peerConnection?.Dispose();
            _peerConnection = null;
        }

        //TODO: some remote peer id variant (maybe _peerConnection.RemoteDescription.UsernameFragment)
        public override string GetRemotePeerId() => throw new NotImplementedException();

        public override void Send(byte[] bytes)
        {
            //Slog.Info($"UnityRtcLink: Send: {bytes.Length} bytes");
            if (_dataChannel != null)
                _dataChannel.Send(bytes);
            else
                _log.Error("no data channel yet (TODO: wait on connect)");
        }
    }
}
#endif
