#nullable enable
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
    public class WebglRtcLink : BaseRtcLink
    {
        private static readonly Slog.Area _log = new();

        //pin managed object as pointer in native implementation (speedup managed object association within callbacks)
        private GCHandle _managedHandle;
        private readonly IntPtr _managedPtr;
        private int _nativeHandle = -1;

        private static readonly Dictionary<int, WebglRtcLink> Links = new(); //TODO: pass instance to callbacks

        private static bool TryGetLink(int nativeHandle, out WebglRtcLink link)
            => Links.TryGetValue(nativeHandle, out link);

        public WebglRtcLink(IRtcService service, IRtcReceiver receiver)
            : base(service, receiver)
        {
            _managedHandle = GCHandle.Alloc(this);
            _managedPtr = GCHandle.ToIntPtr(_managedHandle);
            _log.Info($"[{_managedPtr}]");
        }

        public override void Dispose()
        {
            var allocated = _managedHandle.IsAllocated;
            _log.Info($"allocated: {allocated}");
            if (allocated)
            {
                Links.Remove(_nativeHandle);

                WebglRtcNative.RtcClose(_nativeHandle);
                _nativeHandle = -1;

                _managedHandle.Free();
            }
        }

        public override void Send(byte[] bytes)
        {
            //_log.Info($"{bytes.Length} bytes");
            WebglRtcNative.RtcSend(_nativeHandle, bytes, bytes.Length);
        }

        public async Task Connect(CancellationToken cancellationToken)
        {
            var offerStr = await ObtainOffer(cancellationToken);
            _log.Info("requesting");
            _nativeHandle = WebglRtcNative.RtcConnect(_managedPtr, offerStr);
            _log.Info($"result peerId={_nativeHandle}");
            Links.Add(_nativeHandle, this);
        }

        [MonoPInvokeCallback(typeof(Action<int, string>))]
        public static void ConnectAnswerCallback(int peerId, string answerJson)
        {
            _log.Info($"peerId={peerId}: {answerJson}");
            if (TryGetLink(peerId, out var link))
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
            if (TryGetLink(peerId, out var link))
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
            if (TryGetLink(peerId, out var link))
                link.CallReceived(bytes);
            else
                _log.Error($"failed to find peerId={peerId}");
        }
    }
}