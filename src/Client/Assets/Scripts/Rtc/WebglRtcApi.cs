using System;
using System.Collections.Generic;
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
        private static readonly Dictionary<int, WebglRtcLink> Links = new(); //TODO: pass instance to callbacks

        [DllImport("__Internal")]
        private static extern int RtcInit(
            Action<int, string> connectAnswerCallback,
            Action<int, string> connectCandidatesCallback,
            Action<int, string> connectCompleteCallback,
            Action<int, byte[], int> receivedCallback
            );
        [DllImport("__Internal")]
        private static extern int RtcSetAnswerResult(
            int peerId,
            string candidatesListJson
        );

        public WebglRtcApi(IRtcService service)
        {
            _log.Info(".");
            _service = service;
            RtcInit(
                ConnectAnswerCallback,
                ConnectCandidatesCallback,
                ConnectCompleteCallback,
                ReceivedCallback
                );
        }

        async Task<IRtcLink> IRtcApi.Connect(IRtcLink.ReceivedCallback receivedCallback, CancellationToken cancellationToken)
        {
            _log.Info(".");
            var link = new WebglRtcLink(this, _service, receivedCallback);
            await link.Connect(cancellationToken);
            Links.Add(link.PeerId, link);
            return link;
        }

        void IRtcApi.Listen(IRtcApi.ConnectionCallback connectionCallback) => throw new NotSupportedException();

        internal void Remove(WebglRtcLink link)
        {
            Links.Remove(link.PeerId);
        }

        [MonoPInvokeCallback(typeof(Action<int, string>))]
        public static void ConnectAnswerCallback(int peerId, string answerJson)
        {
            _log.Info($"peerId={peerId}: {answerJson}");
            if (Links.TryGetValue(peerId, out var link))
            {
                link.ReportAnswer(answerJson, CancellationToken.None).ContinueWith(t =>
                {
                    var candidatesListJson = t.Result;
                    _log.Info($"RtcSetAnswerResult: peerId={peerId}: {candidatesListJson}");
                    RtcSetAnswerResult(peerId, candidatesListJson);
                }, TaskScheduler.FromCurrentSynchronizationContext());
            }
            else
                _log.Error($"failed to find peerId={peerId}");
        }
        
        [MonoPInvokeCallback(typeof(Action<int, string>))]
        public static void ConnectCandidatesCallback(int peerId, string candidatesJson)
        {
            _log.Info($"peerId={peerId}: {candidatesJson}");
            if (Links.TryGetValue(peerId, out var link))
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
        public static void ConnectCompleteCallback(int peerId, string error)
        {
            if (error != null)
                _log.Error($"failure: peerId={peerId}: {error}");
            else
                _log.Info($"success: peerId={peerId}");
        }
        
        [MonoPInvokeCallback(typeof(Action<int, byte[], int>))]
        public static void ReceivedCallback(
            int peerId,
            [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.U1, SizeParamIndex = 2)] 
            byte[] bytes, int length)
        {
            if (Links.TryGetValue(peerId, out var link))
                link.CallReceived(link, bytes);
            else
                _log.Error($"failed to find peerId={peerId}");
        }
    }
}