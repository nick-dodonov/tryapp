using System;
using System.Runtime.InteropServices;

namespace Client.Rtc
{
    public static class WebglRtcNative
    {
        [DllImport("__Internal")]
        public static extern int RtcInit(
            Action<int, string> connectAnswerCallback,
            Action<int, string> connectCandidatesCallback,
            Action<int, string> connectCompleteCallback,
            Action<int, byte[], int> receivedCallback
        );

        [DllImport("__Internal")]
        public static extern int RtcSetAnswerResult(
            int peerId,
            string candidatesListJson
        );

        [DllImport("__Internal")]
        public static extern int RtcConnect(IntPtr managedPtr, string offer);

        [DllImport("__Internal")]
        public static extern void RtcClose(int peerId);

        [DllImport("__Internal")]
        public static extern void RtcSend(int peerId, byte[] bytes, int size);
    }
}