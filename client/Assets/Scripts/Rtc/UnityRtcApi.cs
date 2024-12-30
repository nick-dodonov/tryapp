#if UNITY_EDITOR || !UNITY_WEBGL
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Shared;
using Shared.Rtc;
using Shared.Web;
using Unity.WebRTC;

namespace Client.Rtc
{
    public class UnityRtcApi : IRtcApi
    {
        private readonly IRtcService _service;

        public UnityRtcApi(IRtcService service)
        {
            StaticLog.Info("UnityRtcApi: ctr");
            //Disabled because Unity Editor crashes (macOS)
            //WebRTC.ConfigureNativeLogging(true, NativeLoggingSeverity.Info);

            _service = service;
        }

        public async Task<IRtcLink> Connect(IRtcLink.ReceivedCallback callback, CancellationToken cancellationToken)
        {
            var link = new UnityRtcLink(_service, callback);
            await link.Connect(cancellationToken);
            return link;
        }
    }
    
    public class UnityRtcLink : BaseRtcLink, IRtcLink
    {
        private RTCPeerConnection _peerConnection;
        private RTCDataChannel _dataChannel;

        public UnityRtcLink(IRtcService service, IRtcLink.ReceivedCallback receivedCallback)
            : base(service, receivedCallback)
        {
        }

        public async Task Connect(CancellationToken cancellationToken)
        {
            var offerStr = await ObtainOffer(cancellationToken);
            var offer = WebSerializer.DeserializeObject<RTCSessionDescription>(offerStr);
            StaticLog.Info($"UnityRtcLink: Connect: {UnityRtcDebug.Describe(offer)}");
            
            _peerConnection = new();
            _peerConnection.OnIceCandidate = candidate => StaticLog.Info($"UnityRtcLink: OnIceCandidate: {UnityRtcDebug.Describe(candidate)}");
            _peerConnection.OnIceConnectionChange = state => StaticLog.Info($"UnityRtcLink: OnIceConnectionChange: {state}");
            _peerConnection.OnIceGatheringStateChange = state => StaticLog.Info($"UnityRtcLink: OnIceGatheringStateChange: {state}");
            _peerConnection.OnConnectionStateChange = state =>
            {
                StaticLog.Info($"UnityRtcLink: OnConnectionStateChange: {state}");
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
                StaticLog.Info($"UnityRtcLink: OnDataChannel: {UnityRtcDebug.Describe(channel)}");
                _dataChannel = channel;
                channel.OnMessage = CallReceived;
                channel.OnOpen = () => StaticLog.Info($"UnityRtcLink: DataChannel: OnOpen: {channel}");
                channel.OnClose = () => StaticLog.Info($"UnityRtcLink: DataChannel: OnClose: {channel}");
                channel.OnError = error => StaticLog.Info($"UnityRtcLink: DataChannel: OnError: {error}");
            };

            await _peerConnection.SetRemoteDescription(ref offer);
            StaticLog.Info("UnityRtcLink: Creating answer");
            var answerOp = _peerConnection.CreateAnswer();
            await answerOp;
            var answer = answerOp.Desc;
            StaticLog.Info($"UnityRtcLink: Created answer: {UnityRtcDebug.Describe(answer)}");
            await _peerConnection.SetLocalDescription(ref answer);
            
            // send answer to remote side and obtain remote ice candidates
            var answerStr = WebSerializer.SerializeObject(answer);
            var candidatesListJson = await ReportAnswer(answerStr, cancellationToken);
            
            // add remote ICE candidates
            var candidatesList = WebSerializer.DeserializeObject<string[]>(candidatesListJson);
            foreach (var candidateJson in candidatesList)
            {
                //StaticLog.Info($"UnityRtcLink: AddIceCandidate: json: {candidateJson}");
                var candidateInit = WebSerializer.DeserializeObject<RTCIceCandidateInit>(candidateJson);
                //StaticLog.Info($"UnityRtcLink: AddIceCandidate: init: {UnityRtcDebug.Describe(candidateInit)}");
                var candidate = new RTCIceCandidate(candidateInit);
                StaticLog.Info($"UnityRtcLink: AddIceCandidate: {UnityRtcDebug.Describe(candidate)}");
                var rc = _peerConnection.AddIceCandidate(candidate);
                if (!rc) StaticLog.Info("UnityRtcLink: AddIceCandidate: FAILED");
            }
            
            //TODO: wait for DataChannel from server is opened
        }

        public void Dispose()
        {
            StaticLog.Info("UnityRtcLink: Dispose");
            _peerConnection?.Dispose();
        }

        public void Send(byte[] bytes)
        {
            //StaticLog.Info($"UnityRtcLink: Send: {bytes.Length} bytes");
            _dataChannel.Send(bytes);
        }
    }
    
    public static class UnityRtcDebug
    {
        public static string Describe(RTCDataChannel channel) 
            => $"id={channel.Id} label={channel.Label} ordered={channel.Ordered} maxRetransmits={channel.MaxRetransmits} protocol={channel.Protocol} negotiated={channel.Negotiated} bufferedAmount={channel.BufferedAmount} readyState={channel.ReadyState}";
        public static string Describe(in RTCSessionDescription description)
            => WebSerializer.SerializeObject(description); //$"type={description.type} sdp={description.sdp}";
        public static string Describe(RTCIceCandidateInit candidate)
            => $"candidate=\"{candidate.candidate}\" sdpMid={candidate.sdpMid} sdpMLineIndex={candidate.sdpMLineIndex}";
        public static string Describe(RTCIceCandidate candidate)
            => $"address={candidate.Address} port={candidate.Port} protocol={candidate.Protocol} candidate=\"{candidate.Candidate}\"";
    }
}
#endif