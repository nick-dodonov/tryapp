#if UNITY_EDITOR || !UNITY_WEBGL
using System;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Shared;
using Shared.Meta.Api;
using Shared.Rtc;
using Shared.Web;
using Unity.WebRTC;

namespace Client.Rtc
{
    public class UnityRtcLink : IRtcLink
    {
        private readonly IMeta _meta;
        private readonly IRtcLink.ReceivedCallback _receivedCallback;
        private RTCPeerConnection _peerConnection;
        private RTCDataChannel _dataChannel;

        public UnityRtcLink(IMeta meta, IRtcLink.ReceivedCallback receivedCallback)
        {
            _meta = meta;
            _receivedCallback = receivedCallback;
        }

        public async Task Connect(CancellationToken cancellationToken)
        {
            var id = Guid.NewGuid().ToString();
            StaticLog.Info($"Requesting offer for id: {id}");
            var offerStr = await _meta.GetOffer(id, cancellationToken);
            var offer = WebSerializer.DeserializeObject<RTCSessionDescription>(offerStr);
            StaticLog.Info($"Obtained offer: {UnityRtcDebug.Describe(offer)}");
            
            _peerConnection = new();
            _peerConnection.OnIceCandidate = candidate => StaticLog.Info($"OnIceCandidate: {UnityRtcDebug.Describe(candidate)}");
            _peerConnection.OnIceConnectionChange = state => StaticLog.Info($"OnIceConnectionChange: {state}");
            _peerConnection.OnIceGatheringStateChange = state => StaticLog.Info($"OnIceGatheringStateChange: {state}");
            _peerConnection.OnConnectionStateChange = state =>
            {
                StaticLog.Info($"OnConnectionStateChange: {state}");
                if (_dataChannel != null)
                {
                    switch (state)
                    {
                        case RTCPeerConnectionState.Disconnected:
                        case RTCPeerConnectionState.Failed:
                        case RTCPeerConnectionState.Closed:
                            _receivedCallback(null);
                            _dataChannel = null;
                            break;
                    }
                }
            };
            _peerConnection.OnDataChannel = channel =>
            {
                StaticLog.Info($"OnDataChannel: {UnityRtcDebug.Describe(channel)}");
                _dataChannel = channel;
                channel.OnMessage = bytes =>
                {
                    // var messageStr = System.Text.Encoding.UTF8.GetString(bytes);
                    // StaticLog.Info($"DataChannel: OnMessage: {messageStr}");
                    _receivedCallback(bytes);
                };
                channel.OnOpen = () => StaticLog.Info($"DataChannel: OnOpen: {channel}");
                channel.OnClose = () => StaticLog.Info($"DataChannel: OnClose: {channel}");
                channel.OnError = error => StaticLog.Info($"DataChannel: OnError: {error}");
            };

            await _peerConnection.SetRemoteDescription(ref offer);
            StaticLog.Info("Creating answer");
            var answerOp = _peerConnection.CreateAnswer();
            await answerOp;
            var answer = answerOp.Desc;
            StaticLog.Info($"Created answer: {UnityRtcDebug.Describe(answer)}");
            await _peerConnection.SetLocalDescription(ref answer);
            
            StaticLog.Info("Posting answer");
            var answerStr = WebSerializer.SerializeObject(answer);
            var candidate = await _meta.SetAnswer(id, answerStr, cancellationToken);
            StaticLog.Info($"Result candidate: {candidate}");    
            
            //TODO: wait for DataChannel from server is opened
        }
        
        public void Dispose()
        {
            _peerConnection?.Dispose();
        }

        public void Send(byte[] bytes)
        {
            _dataChannel.Send(bytes);
        }
    }
    
    public class UnityRtcApi : IRtcApi
    {
        private readonly IMeta _meta;
        private RTCPeerConnection _peerConnection;

        public UnityRtcApi(IMeta meta)
        {
            StaticLog.Info("UnityRtcClient: created");
            //Disabled because Unity Editor crashes (macOS)
            //WebRTC.ConfigureNativeLogging(true, NativeLoggingSeverity.Info);

            _meta = meta;
        }

        public async Task<IRtcLink> Connect(IRtcLink.ReceivedCallback callback, CancellationToken cancellationToken)
        {
            var link = new UnityRtcLink(_meta, callback);
            await link.Connect(cancellationToken);
            return link;
        }
    }

    public static class UnityRtcDebug
    {
        public static string Describe(RTCDataChannel channel) 
            => $"id={channel.Id} label={channel.Label} ordered={channel.Ordered} maxRetransmits={channel.MaxRetransmits} protocol={channel.Protocol} negotiated={channel.Negotiated} bufferedAmount={channel.BufferedAmount} readyState={channel.ReadyState}";

        public static string Describe(in RTCSessionDescription description)
            => WebSerializer.SerializeObject(description); //$"type={description.type} sdp={description.sdp}";

        public static string Describe(RTCIceCandidate candidate)
            => $"address={candidate.Address} port={candidate.Port} protocol={candidate.Protocol} candidate={candidate.Candidate}";
    }

}
#endif