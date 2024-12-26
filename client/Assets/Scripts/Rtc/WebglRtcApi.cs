using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using AOT;
using Shared;
using Shared.Meta.Api;
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
        private readonly IMeta _meta;

        [DllImport("__Internal")]
        private static extern void SetupTestCallbackString(string message, Action<string> action);

        [DllImport("__Internal")]
        private static extern void SetupTestCallbackBytes(byte[] bytes, int size, Action<byte[], int> action);
        
        public WebglRtcApi(IMeta meta)
        {
            StaticLog.Info("WebglRtcApi: created");
            _meta = meta;
        }

        Task<IRtcLink> IRtcApi.Connect(IRtcLink.ReceivedCallback receivedCallback, CancellationToken cancellationToken)
        {
            StaticLog.Info("WebglRtcApi: Connect");
            TestCallbacks();
            return Task.FromResult<IRtcLink>(new WebglRtcLink(receivedCallback));
            
            // var link = new WebglRtcLink(receivedCallback);
            // await link.Connect(cancellationToken);
            // return link;
        }

        private static void TestCallbacks()
        {
            StaticLog.Info("WebglRtcApi: TestCallbacks");
            SetupTestCallbackString("test-string", TestCallbackString);

            var bytes = new byte[] { 1, 2, 3, 4, 5 };
            SetupTestCallbackBytes(bytes, bytes.Length, TestCallbackBytes);
            for (var i = 0; i < bytes.Length; ++ i)
                bytes[i] += 10;
        }
        
        [MonoPInvokeCallback(typeof(Action<string>))]
        public static void TestCallbackString(string message) 
            => StaticLog.Info($"WebglRtcApi: TestCallbackString: \"{message}\"");

        [MonoPInvokeCallback(typeof(Action<byte[]>))]
        public static void TestCallbackBytes(
            [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.U1, SizeParamIndex = 1)]
            byte[] bytes, int length) =>
            StaticLog.Info($"WebglRtcApi: TestCallbackBytes: [{string.Join(',', bytes)}]");
    }

    public class WebglRtcLink : IRtcLink
    {
        private readonly IRtcLink.ReceivedCallback _receivedCallback;

        [DllImport("__Internal")]
        private static extern void RtcConnect(Action<byte[], int> receivedCallback);

        public WebglRtcLink(IRtcLink.ReceivedCallback receivedCallback)
        {
            _receivedCallback = receivedCallback;
        }

        public void Dispose() { }

        public void Send(byte[] bytes)
        {
            //throw new NotImplementedException();
        }

        public Task Connect(CancellationToken cancellationToken)
        {
            RtcConnect(ReceivedCallback);
            return Task.CompletedTask;
        }
        
        [MonoPInvokeCallback(typeof(Action<byte[]>))]
        public static void ReceivedCallback(
            [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.U1, SizeParamIndex = 1)] 
            byte[] bytes, int length)
        {
            StaticLog.Info($"WebglRtcLink: ReceivedCallback: [{bytes.Length}]");
            //_receivedCallback(bytes);
        }
    }
}