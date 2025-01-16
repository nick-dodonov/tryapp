#nullable enable
using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using AOT;
using Shared.Log;
using Shared.Rtc;

namespace Client.Rtc
{
    /// <summary>
    /// Interaction with browser scripting:
    ///     https://docs.unity3d.com/Manual/webgl-interactingwithbrowserscripting.html
    ///     https://discussions.unity.com/t/send-byte-array-from-js-to-unity/874743/18
    /// </summary>
    public class WebglRtcApi : IRtcApi
    {
        private static readonly Slog.Area _log = new();
        
        private readonly IRtcService _service;

        public WebglRtcApi(IRtcService service)
        {
            _log.Info(".");
            _service = service;
            WebglRtcNative.RtcInit(
                ConnectAnswerCallback,
                ConnectCandidatesCallback,
                ConnectCompleteCallback,
                ReceivedCallback
                );
        }

        async Task<IRtcLink> IRtcApi.Connect(IRtcReceiver receiver, CancellationToken cancellationToken)
        {
            _log.Info(".");
            var link = new WebglRtcLink(_service, receiver);
            await link.Connect(cancellationToken);
            return link;
        }

        void IRtcApi.Listen(IRtcListener listener) => throw new NotSupportedException("server side not implemented");

        [MonoPInvokeCallback(typeof(Action<int, string>))]
        public static void ConnectAnswerCallback(int peerId, string answerJson)
        {
            _log.Info($"peerId={peerId}: {answerJson}");
            if (WebglRtcLink.TryGetLink(peerId, out var link))
            {
                link.ReportAnswer(answerJson, CancellationToken.None).ContinueWith(t =>
                {
                    var candidatesListJson = t.Result;
                    _log.Info($"RtcSetAnswerResult: peerId={peerId}: {candidatesListJson}");
                    WebglRtcNative.RtcSetAnswerResult(peerId, candidatesListJson);
                }, TaskScheduler.FromCurrentSynchronizationContext());
            }
            else
                _log.Error($"failed to find peerId={peerId}");
        }
        
        [MonoPInvokeCallback(typeof(Action<int, string>))]
        public static void ConnectCandidatesCallback(int peerId, string candidatesJson)
        {
            _log.Info($"peerId={peerId}: {candidatesJson}");
            if (WebglRtcLink.TryGetLink(peerId, out var link))
            {
                link.ReportIceCandidates(candidatesJson, CancellationToken.None).ContinueWith(t =>
                {
                    var status = t.Status;
                    _log.Info($"ReportIceCandidates: peerId={peerId}: {status}");
                }, TaskScheduler.FromCurrentSynchronizationContext());
            }
            else
                _log.Error($"failed to find peerId={peerId}");
        }

        [MonoPInvokeCallback(typeof(Action<int, string>))]
        public static void ConnectCompleteCallback(int peerId, string? error)
        {
            if (error != null)
                _log.Error($"failure: peerId={peerId}: {error}");
            else
                _log.Info($"success: peerId={peerId}");
        }
        
        [MonoPInvokeCallback(typeof(Action<int, byte[]?, int>))]
        public static void ReceivedCallback(
            int peerId,
            [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.U1, SizeParamIndex = 2)] 
            byte[]? bytes, int length)
        {
            if (WebglRtcLink.TryGetLink(peerId, out var link))
                link.CallReceived(bytes);
            else
                _log.Error($"failed to find peerId={peerId}");
        }
    }
}