using System;
using System.Runtime.InteropServices;

namespace Shared.Tp.Rtc.Webgl
{
    public static class WebglRtcNative
    {
        [DllImport("__Internal")]
        public static extern int RtcInit(
            Action<IntPtr, string> connectAnswerCallback,
            Action<IntPtr, string> connectCandidatesCallback,
            Action<IntPtr, string> connectCompleteCallback,
            Action<IntPtr, byte[], int> receivedCallback
        );

        [DllImport("__Internal")]
        public static extern int RtcConnect(IntPtr managedPtr, string offer);

        [DllImport("__Internal")]
        public static extern int RtcSetAnswerResult(
            int peerId,
            string candidatesListJson
        );

        [DllImport("__Internal")]
        public static extern void RtcClose(int peerId);

        [DllImport("__Internal")]
        public static extern void RtcSend(int peerId, byte[] bytes, int size);
    }
}