using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using AOT;
using Shared;
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

        [DllImport("__Internal")]
        private static extern int RtcInit(Action<int, string> connectCallback);
        
        public WebglRtcApi(IRtcService service)
        {
            StaticLog.Info("WebglRtcApi: ctr");
            _service = service;
            RtcInit(ConnectCallback);
        }

        async Task<IRtcLink> IRtcApi.Connect(IRtcLink.ReceivedCallback receivedCallback, CancellationToken cancellationToken)
        {
            StaticLog.Info("WebglRtcApi: Connect");
            var link = new WebglRtcLink(_service, receivedCallback);
            await link.Connect(cancellationToken);
            return link;
        }
        
        [MonoPInvokeCallback(typeof(Action<int, string>))]
        public static void ConnectCallback(int peerId, string error)
        {
            StaticLog.Info(error != null
                ? $"WebglRtcApi: ConnectCallback: failure: peerId={peerId} {error}"
                : $"WebglRtcApi: ConnectCallback: success: peerId={peerId}");
        }
    }

    public class WebglRtcLink : BaseRtcLink, IRtcLink
    {
        [DllImport("__Internal")]
        private static extern int RtcConnect(string offer, Action<byte[], int> receivedCallback);

        public WebglRtcLink(IRtcService service, IRtcLink.ReceivedCallback receivedCallback)
            : base(service, receivedCallback)
        {
        }

        public void Dispose()
        {
            //throw new NotImplementedException();
        }

        public void Send(byte[] bytes)
        {
            //throw new NotImplementedException();
        }

        public async Task Connect(CancellationToken cancellationToken)
        {
            var offerStr = await ObtainOffer(cancellationToken);
            StaticLog.Info("WebglRtcLink: Connect: request");
            var result = RtcConnect(offerStr, ReceivedCallback);
            StaticLog.Info($"WebglRtcLink: Connect: result: {result}");
        }

        [MonoPInvokeCallback(typeof(Action<byte[]>))]
        public static void ReceivedCallback(
            [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.U1, SizeParamIndex = 1)] 
            byte[] bytes, int length)
        {
            if (bytes != null)
                StaticLog.Info($"WebglRtcLink: ReceivedCallback: [{string.Join(',', bytes)}]");
            else
                StaticLog.Info($"WebglRtcLink: ReceivedCallback: disconnected");
            //CallReceived(bytes);
            //_receivedCallback(bytes);
        }
    }
}