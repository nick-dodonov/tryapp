using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using AOT;
using Shared;
using Shared.Meta.Api;

namespace Rtc
{
    /// <summary>
    /// Interaction with browser scripting:
    ///     https://docs.unity3d.com/Manual/webgl-interactingwithbrowserscripting.html
    /// </summary>
    public class WebglRtcClient : IRtcClient
    {
        private readonly IMeta _meta;

        [DllImport("__Internal")]
        private static extern void Hello();
        [DllImport("__Internal")]
        private static extern void Connect();

        [DllImport("__Internal")]
        private static extern void SetupTestCallback(string message, Action<string> action);
        
        public WebglRtcClient(IMeta meta)
        {
            StaticLog.Info("WebglRtcClient: created");
            _meta = meta;
        }

        [MonoPInvokeCallback(typeof(Action<string>))]
        public static void TestCallback(string message)
        {
            StaticLog.Info($"WebglRtcClient: TestCallback: \"{message}\"");
        }

        Task<IRtcLink> IRtcClient.Connect(IRtcLink.ReceivedCallback receivedCallback, CancellationToken cancellationToken)
        {
            StaticLog.Info("Connect: TODO");
            throw new NotImplementedException();
        }
    }
}