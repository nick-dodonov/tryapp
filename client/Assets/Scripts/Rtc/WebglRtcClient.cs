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
        [DllImport("__Internal")]
        private static extern void Hello();
        [DllImport("__Internal")]
        private static extern void SetupTestCallback(string message, Action<string> action);
        
        public WebglRtcClient()
        {
            StaticLog.Info("WebglRtcClient: created");
        }

        public async Task<string> TryCall(IMeta meta, CancellationToken cancellationToken)
        {
            StaticLog.Info("WebglRtcClient: TryCall: TODO");
            await Task.Yield();
            
            //Hello();
            SetupTestCallback("message-into-js", TestCallback);
            
            return "TODO";
        }
        
        [MonoPInvokeCallback(typeof(Action<string>))]
        public static void TestCallback(string message)
        {
            StaticLog.Info($"WebglRtcClient: TestCallback: \"{message}\"");
        }
    }
}