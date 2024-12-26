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

        public WebglRtcApi(IRtcService service)
        {
            StaticLog.Info("WebglRtcApi: ctr");
            _service = service;
        }

        async Task<IRtcLink> IRtcApi.Connect(IRtcLink.ReceivedCallback receivedCallback, CancellationToken cancellationToken)
        {
            StaticLog.Info("WebglRtcApi: Connect");
            var link = new WebglRtcLink(_service, receivedCallback);
            await link.Connect(cancellationToken);
            return link;
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
            StaticLog.Info($"WebglRtcLink: Connect: request");
            var result = RtcConnect(offerStr, ReceivedCallback);
            StaticLog.Info($"WebglRtcLink: Connect: result: {result}");
        }
        
        [MonoPInvokeCallback(typeof(Action<byte[]>))]
        public static void ReceivedCallback(
            [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.U1, SizeParamIndex = 1)] 
            byte[] bytes, int length)
        {
            StaticLog.Info($"WebglRtcLink: ReceivedCallback: [{string.Join(',', bytes)}]");
            //CallReceived(bytes);
            //_receivedCallback(bytes);
        }
    }
}