#if UNITY_5_6_OR_NEWER && (UNITY_EDITOR || !UNITY_WEBGL)
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Shared.Tp.Util;
using Unity.WebRTC;

namespace Shared.Tp.Rtc.Unity
{
    public class UnityRtcLink : BaseRtcLink
    {
        private RTCPeerConnection? _peerConnection;
        private RTCDataChannel? _dataChannel;
        private readonly List<RtcIcInit> _iceCandidates = new();

        private readonly TaskCompletionSource<RTCDataChannel> _dataChannelTcs = new();

        public UnityRtcLink(IRtcService service, ITpReceiver receiver)
            : base(service, receiver) { }

        public override string ToString() => $"{nameof(UnityRtcLink)}<{LinkId}>"; //only for diagnostics

        public async Task Connect(CancellationToken cancellationToken)
        {
            Log.Info(".");
            var offer = await ObtainOffer(cancellationToken);

            var offerSdp = offer.SdpInit.FromShared();

            var configuration = new RTCConfiguration()
            {
                iceServers = offer.Config?.IceServers?.Select(x => new RTCIceServer
                {
                    urls = new[] { x.Url },
                    username = x.Username,
                    credential = x.Password,
                }).ToArray()
            };

            var iceServersStr = string.Join(", ", configuration.iceServers?.Select(x => $"\"{x.urls[0]}\"") ?? Enumerable.Empty<string>());
            Log.Info($"creating peer connection: [{iceServersStr}]");
            _peerConnection = new(ref configuration);

            _peerConnection.OnIceCandidate = candidate =>
            {
                var sharedCandidate = candidate.ToShared();
                Log.Info($"OnIceCandidate: {sharedCandidate}");
                _iceCandidates.Add(sharedCandidate);
            };
            // ReSharper disable once AsyncVoidLambda
            _peerConnection.OnIceGatheringStateChange = async state =>
            {
                try
                {
                    Log.Info($"OnIceGatheringStateChange: {state}");
                    if (state == RTCIceGatheringState.Complete)
                        await ReportIceCandidates(_iceCandidates.ToArray(), cancellationToken);
                }
                catch (Exception ex)
                {
                    Log.Error($"OnIceGatheringStateChange: ReportIceCandidates: failed: {ex}");
                }
            };
            _peerConnection.OnIceConnectionChange = state =>
                Log.Info($"OnIceConnectionChange: {state}");

            _peerConnection.OnConnectionStateChange = state =>
            {
                Log.Info($"OnConnectionStateChange: {state}");
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
                Log.Info($"OnDataChannel: {Describe(channel)}");
                channel.OnMessage = CallReceived;
                channel.OnOpen = () => Log.Info($"OnDataChannel: OnOpen: {channel}");
                channel.OnClose = () => Log.Info($"OnDataChannel: OnClose: {channel}");
                channel.OnError = error => Log.Info($"OnDataChannel: OnError: {error}");
                _dataChannel = channel;
                _dataChannelTcs.TrySetResult(channel);
            };

            Log.Info($"setting remote sdp: {offerSdp.type}\n{offerSdp.sdp}");
            await _peerConnection.SetRemoteDescription(ref offerSdp);

            Log.Info("creating answer sdp");
            var answerOp = _peerConnection.CreateAnswer();
            await answerOp;
            var answer = answerOp.Desc;

            Log.Info($"setting answer sdp: {answer.type}\n{answer.sdp}");
            await _peerConnection.SetLocalDescription(ref answer);

            // send answer to remote side and obtain remote ice candidates
            var candidates = await ReportAnswer(answer.ToShared(), cancellationToken);

            // add remote ICE candidates
            foreach (var candidate in candidates)
            {
                Log.Info($"adding remote ice candidate: {candidate}");
                var unityCandidateInit = candidate.FromShared();
                var unityCandidate = new RTCIceCandidate(unityCandidateInit);
                var rc = _peerConnection.AddIceCandidate(unityCandidate);
                if (!rc)
                    Log.Error("FAILED to add remote ice candidate");
            }

            Log.Info("awaiting data channel");
            await _dataChannelTcs.Task.WaitAsync(cancellationToken);
            Log.Info("connection established");
        }

        public override void Dispose()
        {
            Log.Info("Dispose");
            _dataChannelTcs.TrySetCanceled();
            _peerConnection?.Dispose();
            _peerConnection = null;
        }

        //TODO: some remote peer id variant (maybe _peerConnection.RemoteDescription.UsernameFragment)
        public override string GetRemotePeerId() => throw new NotImplementedException();

        private unsafe void Send(ReadOnlySpan<byte> span)
        {
            //Slog.Info($"UnityRtcLink: Send: {bytes.Length} bytes");
            if (_dataChannel != null)
                fixed (byte* ptr = span)
                {
                    _dataChannel.Send(ptr, span.Length); 
                }
            else
                Log.Error("no data channel yet (TODO: wait on connect)");
        }

        public override void Send<T>(TpWriteCb<T> writeCb, in T state)
        {
            using var writer = PooledBufferWriter.Rent();
            writeCb(writer, state);
            Send(writer.WrittenSpan);
        }

        private static string Describe(RTCDataChannel channel)
            =>
                $"Id={channel.Id} Label={channel.Label} ReadyState={channel.ReadyState} Protocol={channel.Protocol} Ordered={channel.Ordered} MaxRetransmits={channel.MaxRetransmits} MaxRetransmitTime={channel.MaxRetransmitTime} Negotiated={channel.Negotiated} BufferedAmount={channel.BufferedAmount}";
    }
}
#endif