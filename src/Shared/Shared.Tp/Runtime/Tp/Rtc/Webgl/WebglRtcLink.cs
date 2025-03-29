#if UNITY_5_6_OR_NEWER
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using AOT;
using Shared.Log;
using Shared.Tp.Util;
using Shared.Web;

namespace Shared.Tp.Rtc.Webgl
{
    public class WebglRtcLink : BaseRtcLink
    {
        //pin managed object as pointer in native implementation (speedup managed object association within callbacks)
        private GCHandle _managedHandle;
        private readonly IntPtr _managedPtr;
        private int _nativeHandle = -1;

        private readonly TaskCompletionSource<object?> _connectTcs = new();

        public WebglRtcLink(IRtcService service, ITpReceiver receiver)
            : base(service, receiver)
        {
            _managedHandle = GCHandle.Alloc(this);
            _managedPtr = GCHandle.ToIntPtr(_managedHandle);

            Log.Info($"managedPtr={_managedPtr}");
        }

        public override string ToString() => $"{nameof(WebglRtcLink)}<{_nativeHandle}/{LinkId}>"; //only for diagnostics

        public override void Dispose()
        {
            var allocated = _managedHandle.IsAllocated;
            Log.Info($"managedPtr={_managedPtr} allocated={allocated}");
            if (!allocated)
                return;

            _connectTcs.TrySetCanceled();

            WebglRtcNative.RtcClose(_nativeHandle);
            _nativeHandle = -1;

            _managedHandle.Free();
        }

        //TODO: some remote peer id variant (maybe _peerConnection.RemoteDescription.UsernameFragment)
        public override string GetRemotePeerId() => throw new NotImplementedException();

        private void Send(ReadOnlySpan<byte> span)
        {
            //Log.Info($"{bytes.Length} bytes");
            var bytes = span.ToArray(); //TODO: speedup: make try to pass span
            WebglRtcNative.RtcSend(_nativeHandle, bytes, bytes.Length);
        }

        public override void Send<T>(TpWriteCb<T> writeCb, in T state)
        {
            using var writer = PooledBufferWriter.Rent();
            writeCb(writer, state);
            Send(writer.WrittenSpan);
        }

        public async Task Connect(CancellationToken cancellationToken)
        {
            Log.Info(".");
            var offer = await ObtainOffer(cancellationToken);
            _nativeHandle = WebglRtcNative.RtcConnect(_managedPtr, 
                offer.SdpInit.ToJson(),
                offer.Config?.ToJson());

            Log.Info($"result nativeHandle={_nativeHandle}, awaiting opened channel");
            await _connectTcs.Task;

            Log.Info("connection established");
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
                Log.Info(answerJson);
                var answerSdp = RtcSdpInit.FromJson(answerJson);
                ReportAnswer(answerSdp, CancellationToken.None).ContinueWith(t =>
                {
                    //TODO: handle connection error
                    var candidates = t.Result;

                    Log.Info($"ReportAnswer: [{candidates.Length}] candidates");
                    foreach (var candidate in candidates)
                        WebglRtcNative.RtcAddIceCandidate(_nativeHandle, candidate.ToJson());
                }, TaskScheduler.FromCurrentSynchronizationContext());
            }
            catch (Exception e)
            {
                Log.Error($"{e}");
            }
        }

        [MonoPInvokeCallback(typeof(Action<IntPtr, string>))]
        public static void ConnectCandidatesCallback(IntPtr managedPtr, string candidatesJson)
            => GetLink(managedPtr)?.CallReportIceCandidates(candidatesJson);

        private void CallReportIceCandidates(string candidatesJson)
        {
            try
            {
                Log.Info(candidatesJson);
                var candidates = WebSerializer.Default.Deserialize<RtcIcInit[]>(candidatesJson);
                Log.Info($"ReportIceCandidates: [{candidates.Length}] candidates");
                ReportIceCandidates(candidates, CancellationToken.None).ContinueWith(t =>
                {
                    //TODO: handle connection error
                    var status = t.Status;
                    Log.Info($"ReportIceCandidates: status: {status}");
                }, TaskScheduler.FromCurrentSynchronizationContext());
            }
            catch (Exception e)
            {
                Log.Error($"{e}");
            }
        }

        [MonoPInvokeCallback(typeof(Action<IntPtr, string>))]
        public static void ConnectCompleteCallback(IntPtr managedPtr, string? error)
            => GetLink(managedPtr)?.CallConnectComplete(error);

        private void CallConnectComplete(string? error)
        {
            if (error == null)
            {
                Log.Info("success");
                _connectTcs.SetResult(null);
            }
            else
            {
                Log.Error(error);
                _connectTcs.SetException(new Exception(error));
            }
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