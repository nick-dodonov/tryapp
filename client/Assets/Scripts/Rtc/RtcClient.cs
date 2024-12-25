using System;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Shared;
using Shared.Meta.Api;
using Unity.WebRTC;
using UnityEngine;

namespace Rtc
{
    public class RtcClient
    {
        private RTCPeerConnection _peerConnection;

        public async Task<string> TryCall(IMeta meta, CancellationToken cancellationToken)
        {
            var id = Guid.NewGuid().ToString();
            StaticLog.Info($"Requesting offer for id: {id}");
            var offerStr = await meta.GetOffer(id, cancellationToken);
            var offer = JsonUtility.FromJson<RTCSessionDescription>(offerStr);
            StaticLog.Info($"Obtained offer: {Describe(offer)}");
            
            _peerConnection = new();
            _peerConnection.OnIceCandidate = candidate => StaticLog.Info($"OnIceCandidate: {Describe(candidate)}");
            _peerConnection.OnIceConnectionChange = state => StaticLog.Info($"OnIceConnectionChange: {state}");
            _peerConnection.OnIceGatheringStateChange = state => StaticLog.Info($"OnIceGatheringStateChange: {state}");
            _peerConnection.OnDataChannel = channel =>
            {
                StaticLog.Info($"OnDataChannel: {channel}");
                channel.OnMessage = message =>
                {
                    var messageStr = message.ToString();
                    StaticLog.Info($"OnMessage: {messageStr}");
                };
                channel.OnOpen = () => StaticLog.Info($"OnOpen: {channel}");
                channel.OnClose = () => StaticLog.Info($"OnClose: {channel}");
                channel.OnError = error => StaticLog.Info($"OnError: {error}");
            };

            await _peerConnection.SetRemoteDescription(ref offer);
            StaticLog.Info("Creating answer");
            var answerOp = _peerConnection.CreateAnswer();
            await answerOp;
            var answer = answerOp.Desc;
            StaticLog.Info($"Created answer: {Describe(answer)}");
            await _peerConnection.SetLocalDescription(ref answer);
            
            StaticLog.Info("Posting answer");
            var answerStr = JsonUtility.ToJson(answer);
            var candidate = await meta.SetAnswer(id, answerStr, cancellationToken);
            StaticLog.Info($"Result candidate: {candidate}");
            
            StaticLog.Info("Calling completed");
            return "OK";
        }

        private static string Describe(in RTCSessionDescription sessionDescription)
            => $"type={sessionDescription.type} sdp={sessionDescription.sdp}";

        private static string Describe(RTCIceCandidate candidate) 
            => $"address={candidate.Address} port={candidate.Port} protocol={candidate.Protocol} candidate={candidate.Candidate}";
    }
}