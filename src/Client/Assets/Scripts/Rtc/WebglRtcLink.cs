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
    public class WebglRtcLink : BaseRtcLink
    {
        private static readonly Slog.Area _log = new();

        //pin managed object as pointer in native implementation (speedup managed object association within callbacks)
        private GCHandle _managedHandle;
        private readonly IntPtr _managedPtr;
        private int _nativeHandle = -1;

        public WebglRtcLink(IRtcService service, IRtcReceiver receiver)
            : base(service, receiver)
        {
            _managedHandle = GCHandle.Alloc(this);
            _managedPtr = GCHandle.ToIntPtr(_managedHandle);

            _log.Info($"managedPtr={_managedPtr}");
        }

        private static bool TryGetLink(IntPtr managerPtr, out WebglRtcLink link)
        {
            var handle = GCHandle.FromIntPtr(managerPtr);
            if (handle.IsAllocated)
            {
                link = (WebglRtcLink)handle.Target;
                return true;
            }

            link = null!;
            return false;
        }

        public override void Dispose()
        {
            var allocated = _managedHandle.IsAllocated;
            _log.Info($"managedPtr={_managedPtr} allocated={allocated}");
            if (!allocated)
                return;

            WebglRtcNative.RtcClose(_nativeHandle);
            _nativeHandle = -1;

            _managedHandle.Free();
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
            _log.Info($"result nativeHandle={_nativeHandle}");
        }

        [MonoPInvokeCallback(typeof(Action<IntPtr, string>))]
        public static void ConnectAnswerCallback(IntPtr managedPtr, string answerJson)
        {
            _log.Info($"managedPtr={managedPtr}: {answerJson}");
            if (TryGetLink(managedPtr, out var link))
            {
                link.ReportAnswer(answerJson, CancellationToken.None).ContinueWith(t =>
                {
                    var candidatesListJson = t.Result;
                    _log.Info($"RtcSetAnswerResult: managedPtr={managedPtr}: {candidatesListJson}");
                    WebglRtcNative.RtcSetAnswerResult(link._nativeHandle, candidatesListJson);
                }, TaskScheduler.FromCurrentSynchronizationContext());
            }
            else
                _log.Error($"failed to find managedPtr={managedPtr}");
        }

        [MonoPInvokeCallback(typeof(Action<IntPtr, string>))]
        public static void ConnectCandidatesCallback(IntPtr managedPtr, string candidatesJson)
        {
            _log.Info($"managedPtr={managedPtr}: {candidatesJson}");
            if (TryGetLink(managedPtr, out var link))
            {
                link.ReportIceCandidates(candidatesJson, CancellationToken.None).ContinueWith(t =>
                {
                    var status = t.Status;
                    _log.Info($"ReportIceCandidates: managedPtr={managedPtr}: {status}");
                }, TaskScheduler.FromCurrentSynchronizationContext());
            }
            else
                _log.Error($"failed to find managedPtr={managedPtr}");
        }

        [MonoPInvokeCallback(typeof(Action<IntPtr, string>))]
        public static void ConnectCompleteCallback(IntPtr managedPtr, string? error)
        {
            if (error != null)
                _log.Error($"failure: managedPtr={managedPtr}: {error}");
            else
                _log.Info($"success: managedPtr={managedPtr}");
        }

        [MonoPInvokeCallback(typeof(Action<IntPtr, byte[]?, int>))]
        public static void ReceivedCallback(
            IntPtr managedPtr,
            [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.U1, SizeParamIndex = 2)]
            byte[]? bytes, int length)
        {
            if (TryGetLink(managedPtr, out var link))
                link.CallReceived(bytes);
            else
                _log.Error($"failed to find managedPtr={managedPtr}");
        }
    }
}