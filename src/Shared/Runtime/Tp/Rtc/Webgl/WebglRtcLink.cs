#if UNITY_5_6_OR_NEWER
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using AOT;
using Shared.Log;
using Shared.Web;

namespace Shared.Tp.Rtc.Webgl
{
    public class WebglRtcLink : BaseRtcLink
    {
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

        //TODO: some remote peer id variant (maybe _peerConnection.RemoteDescription.UsernameFragment)
        public override string GetRemotePeerId() => throw new NotImplementedException();

        public override void Send(byte[] bytes)
        {
            //_log.Info($"{bytes.Length} bytes");
            WebglRtcNative.RtcSend(_nativeHandle, bytes, bytes.Length);
        }

        public async Task Connect(CancellationToken cancellationToken)
        {
            _log.Info(".");
            var offer = await ObtainOffer(cancellationToken);
            _nativeHandle = WebglRtcNative.RtcConnect(_managedPtr, offer.ToJson());
            _log.Info($"result nativeHandle={_nativeHandle}");
        }

        private static WebglRtcLink? GetLink(IntPtr managedPtr, [CallerMemberName] string member = "")
        {
            var handle = GCHandle.FromIntPtr(managedPtr);
            if (handle.IsAllocated)
                return (WebglRtcLink)handle.Target;

            Slog.Error($"GetLink failed managedPtr={managedPtr}", member: member);
            return null;
        }
        
        [MonoPInvokeCallback(typeof(Action<IntPtr, string>))]
        public static void ConnectAnswerCallback(IntPtr managedPtr, string answerJson) 
            => GetLink(managedPtr)?.CallReportAnswer(answerJson);

        private void CallReportAnswer(string answerJson)
        {
            try
            {
                _log.Info(answerJson);
                var answerSdp = RtcSdpInit.FromJson(answerJson);
                ReportAnswer(answerSdp, CancellationToken.None).ContinueWith(t =>
                {
                    //TODO: handle connection error
                    var candidates = t.Result;
                    
                    _log.Info($"ReportAnswer: [{candidates.Length}] candidates");
                    foreach (var candidate in candidates)
                        WebglRtcNative.RtcAddIceCandidate(_nativeHandle, candidate.ToJson());
                }, TaskScheduler.FromCurrentSynchronizationContext());
            }
            catch (Exception e)
            {
                _log.Error($"{e}");
            }
        }

        [MonoPInvokeCallback(typeof(Action<IntPtr, string>))]
        public static void ConnectCandidatesCallback(IntPtr managedPtr, string candidatesJson) 
            => GetLink(managedPtr)?.CallReportIceCandidates(candidatesJson);

        private void CallReportIceCandidates(string candidatesJson)
        {
            try
            {
                _log.Info(candidatesJson);
                var candidates = WebSerializer.DeserializeObject<RtcIceCandidate[]>(candidatesJson);
                _log.Info($"ReportIceCandidates: [{candidates.Length}] candidates");
                ReportIceCandidates(candidates, CancellationToken.None).ContinueWith(t =>
                {
                    //TODO: handle connection error
                    var status = t.Status;
                    _log.Info($"ReportIceCandidates: status: {status}");
                }, TaskScheduler.FromCurrentSynchronizationContext());
            }
            catch (Exception e)
            {
                _log.Error($"{e}");
            }
        }

        [MonoPInvokeCallback(typeof(Action<IntPtr, string>))]
        public static void ConnectCompleteCallback(IntPtr managedPtr, string? error)
        {
            if (error != null)
                Slog.Error($"failure: managedPtr={managedPtr}: {error}");
            else
                Slog.Info($"success: managedPtr={managedPtr}");
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
#endif