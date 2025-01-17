#nullable enable
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using AOT;
using Shared.Log;
using Shared.Rtc;
using Shared.Tp;

namespace Client.Rtc
{
    public class WebglRtcLink : BaseRtcLink
    {
        private static readonly Slog.Area _log = new();

        //pin managed object as pointer in native implementation (speedup managed object association within callbacks)
        private GCHandle _managedHandle;
        private readonly IntPtr _managedPtr;
        private int _nativeHandle = -1;

        public WebglRtcLink(IRtcService service, ITpReceiver receiver)
            : base(service, receiver)
        {
            _managedHandle = GCHandle.Alloc(this);
            _managedPtr = GCHandle.ToIntPtr(_managedHandle);

            _log.Info($"managedPtr={_managedPtr}");
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

        private static WebglRtcLink? GetLink(IntPtr managedPtr, [CallerMemberName] string member = "")
        {
            var handle = GCHandle.FromIntPtr(managedPtr);
            if (handle.IsAllocated)
                return (WebglRtcLink)handle.Target;

            _log.Error($"GetLink failed managedPtr={managedPtr}", member: member);
            return null;
        }
        
        [MonoPInvokeCallback(typeof(Action<IntPtr, string>))]
        public static void ConnectAnswerCallback(IntPtr managedPtr, string answerJson) 
            => GetLink(managedPtr)?.CallReportAnswer(answerJson);

        private void CallReportAnswer(string answerJson)
        {
            ReportAnswer(answerJson, CancellationToken.None).ContinueWith(t =>
            {
                var candidatesListJson = t.Result; 
                //TODO: handle connection error
                _log.Info($"ReportAnswer: {candidatesListJson}");
                WebglRtcNative.RtcSetAnswerResult(_nativeHandle, candidatesListJson);
            }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        [MonoPInvokeCallback(typeof(Action<IntPtr, string>))]
        public static void ConnectCandidatesCallback(IntPtr managedPtr, string candidatesJson) 
            => GetLink(managedPtr)?.CallReportIceCandidates(candidatesJson);

        private void CallReportIceCandidates(string candidatesJson)
        {
            ReportIceCandidates(candidatesJson, CancellationToken.None).ContinueWith(t =>
            {
                var status = t.Status;
                //TODO: handle connection error
                _log.Info($"ReportIceCandidates: managedPtr={_managedPtr}: {status}");
            }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        [MonoPInvokeCallback(typeof(Action<IntPtr, string>))]
        public static void ConnectCompleteCallback(IntPtr managedPtr, string? error)
        {
            if (error != null)
                _log.Error($"failure: managedPtr={managedPtr}: {error}");
            else
                _log.Info($"success: managedPtr={managedPtr}");
            //TODO: add Task await on connect
        }

        [MonoPInvokeCallback(typeof(Action<IntPtr, byte[]?, int>))]
        public static void ReceivedCallback(
            IntPtr managedPtr,
            [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.U1, SizeParamIndex = 2)]
            byte[]? bytes, int length)
            => GetLink(managedPtr)?.CallReceived(bytes);
    }
}