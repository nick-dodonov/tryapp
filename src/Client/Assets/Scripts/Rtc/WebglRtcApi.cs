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
            Slog.Info(".");
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
            Slog.Info(".");
            var link = new WebglRtcLink(this, _service, receivedCallback);
            await link.Connect(cancellationToken);
            Links.Add(link.PeerId, link);
            return link;
        }

        internal void Remove(WebglRtcLink link)
        {
            Links.Remove(link.PeerId);
        }

        [MonoPInvokeCallback(typeof(Action<int, string>))]
        public static void ConnectAnswerCallback(int peerId, string answerJson)
        {
            Slog.Info($"peerId={peerId}: {answerJson}");
            if (Links.TryGetValue(peerId, out var link))
            {
                link.ReportAnswer(answerJson, CancellationToken.None).ContinueWith(t =>
                {
                    var candidatesListJson = t.Result;
                    Slog.Info($"RtcSetAnswerResult: peerId={peerId}: {candidatesListJson}");
                    RtcSetAnswerResult(peerId, candidatesListJson);
                }, TaskScheduler.FromCurrentSynchronizationContext());
            }
            else
                Slog.Info($"ERROR: failed to find peerId={peerId}");
        }
        
        [MonoPInvokeCallback(typeof(Action<int, string>))]
        public static void ConnectCandidatesCallback(int peerId, string candidatesJson)
        {
            Slog.Info($"peerId={peerId}: {candidatesJson}");
            if (Links.TryGetValue(peerId, out var link))
            {
                link.ReportIceCandidates(candidatesJson, CancellationToken.None).ContinueWith(t =>
                {
                    var status = t.Status;
                    Slog.Info($"ReportIceCandidates: peerId={peerId}: {status}");
                }, TaskScheduler.FromCurrentSynchronizationContext());
            }
            else
                Slog.Info($"ERROR: failed to find peerId={peerId}");
        }

        [MonoPInvokeCallback(typeof(Action<int, string>))]
        public static void ConnectCompleteCallback(int peerId, string error)
        {
            Slog.Info(error != null
                ? $"failure: peerId={peerId} {error}"
                : $"success: peerId={peerId}");
        }
        
        [MonoPInvokeCallback(typeof(Action<int, byte[], int>))]
        public static void ReceivedCallback(
            int peerId,
            [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.U1, SizeParamIndex = 2)] 
            byte[] bytes, int length)
        {
            if (Links.TryGetValue(peerId, out var link))
                link.CallReceived(bytes);
            else
                Slog.Info($"ERROR: failed to find peerId={peerId}");
        }
    }

    public class WebglRtcLink : BaseRtcLink, IRtcLink
    {
        private readonly WebglRtcApi _api;

        private int _peerId = -1;
        public int PeerId => _peerId;

        [DllImport("__Internal")]
        private static extern int RtcConnect(string offer);
        [DllImport("__Internal")]
        private static extern void RtcClose(int peerId);
        [DllImport("__Internal")]
        private static extern void RtcSend(int peerId, byte[] bytes, int size);

        public WebglRtcLink(WebglRtcApi api, IRtcService service, IRtcLink.ReceivedCallback receivedCallback)
            : base(service, receivedCallback)
        {
            _api = api;
        }

        public void Dispose()
        {
            Slog.Info("Dispose");
            if (_peerId >= 0)
            {
                _api.Remove(this);

                RtcClose(_peerId);
                _peerId = -1;
            }
        }

        public void Send(byte[] bytes)
        {
            //Slog.Info($"WebglRtcLink: Send: {bytes.Length} bytes");
            RtcSend(_peerId, bytes, bytes.Length);
        }

        public async Task Connect(CancellationToken cancellationToken)
        {
            var offerStr = await ObtainOffer(cancellationToken);
            Slog.Info("request");
            _peerId = RtcConnect(offerStr);
            Slog.Info($"peerId={_peerId}");
        }
    }
}