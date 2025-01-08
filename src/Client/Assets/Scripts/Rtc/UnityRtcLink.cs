#if UNITY_EDITOR || !UNITY_WEBGL
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Shared.Log;
using Shared.Rtc;
using Shared.Web;
using Unity.WebRTC;

namespace Client.Rtc
{
    public class UnityRtcLink : BaseRtcLink, IRtcLink
    {
        private RTCPeerConnection _peerConnection;
        private RTCDataChannel _dataChannel;
        private readonly List<RTCIceCandidateInit> _iceCandidates = new();

        public UnityRtcLink(IRtcService service, IRtcLink.ReceivedCallback receivedCallback)
            : base(service, receivedCallback)
        {}

        public async Task Connect(CancellationToken cancellationToken)
        {
            var offerStr = await ObtainOffer(cancellationToken);
            var offer = WebSerializer.DeserializeObject<RTCSessionDescription>(offerStr);
            Slog.Info($"{UnityRtcDebug.Describe(offer)}");
            
            _peerConnection = new();
            _peerConnection.OnIceCandidate = candidate =>
            {
                Slog.Info($"OnIceCandidate: {UnityRtcDebug.Describe(candidate)}");
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
                    Slog.Info($"OnIceGatheringStateChange: {state}");
                    if (state == RTCIceGatheringState.Complete)
                    {
                        var candidatesJson = WebSerializer.SerializeObject(_iceCandidates);
                        await ReportIceCandidates(candidatesJson, cancellationToken);
                    }
                }
                catch (Exception ex)
                {
                    Slog.Info($"OnIceGatheringStateChange: ReportIceCandidates: failed: {ex}");
                }
            };
            _peerConnection.OnIceConnectionChange = state => 
                Slog.Info($"OnIceConnectionChange: {state}");
            _peerConnection.OnConnectionStateChange = state =>
            {
                Slog.Info($"OnConnectionStateChange: {state}");
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
                Slog.Info($"OnDataChannel: {UnityRtcDebug.Describe(channel)}");
                _dataChannel = channel;
                channel.OnMessage = CallReceived;
                channel.OnOpen = () => Slog.Info($"OnDataChannel: OnOpen: {channel}");
                channel.OnClose = () => Slog.Info($"OnDataChannel: OnClose: {channel}");
                channel.OnError = error => Slog.Info($"OnDataChannel: OnError: {error}");
            };

            await _peerConnection.SetRemoteDescription(ref offer);
            Slog.Info("Creating answer");
            var answerOp = _peerConnection.CreateAnswer();
            await answerOp;
            var answer = answerOp.Desc;
            Slog.Info($"Created answer: {UnityRtcDebug.Describe(answer)}");
            await _peerConnection.SetLocalDescription(ref answer);
            
            // send answer to remote side and obtain remote ice candidates
            var answerJson = WebSerializer.SerializeObject(answer);
            var candidatesListJson = await ReportAnswer(answerJson, cancellationToken);
            
            // add remote ICE candidates
            var candidatesList = WebSerializer.DeserializeObject<string[]>(candidatesListJson);
            foreach (var candidateJson in candidatesList)
            {
                //Slog.Info($"UnityRtcLink: AddIceCandidate: json: {candidateJson}");
                var candidateInit = WebSerializer.DeserializeObject<RTCIceCandidateInit>(candidateJson);
                //Slog.Info($"UnityRtcLink: AddIceCandidate: init: {UnityRtcDebug.Describe(candidateInit)}");
                var candidate = new RTCIceCandidate(candidateInit);
                Slog.Info($"AddIceCandidate: {UnityRtcDebug.Describe(candidate)}");
                var rc = _peerConnection.AddIceCandidate(candidate);
                if (!rc) 
                    Slog.Info("AddIceCandidate: FAILED");
            }
            
            //TODO: wait for DataChannel from server is opened
        }

        public void Dispose()
        {
            Slog.Info("Dispose");
            _peerConnection?.Dispose();
        }

        public void Send(byte[] bytes)
        {
            //Slog.Info($"UnityRtcLink: Send: {bytes.Length} bytes");
            if (_dataChannel != null)
                _dataChannel.Send(bytes);
            else
                Slog.Info("Send: ERROR: no data channel yet (TODO: wait on connect)");
        }
    }
}
#endif