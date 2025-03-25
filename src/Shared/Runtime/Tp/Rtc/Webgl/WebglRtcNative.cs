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

        /// <summary>
        /// Create WebRTC connection with passed offer sdp 
        /// </summary>
        /// <returns>Native handle to associate with this connection</returns>
        [DllImport("__Internal")]
        public static extern int RtcConnect(IntPtr managedPtr, string offerJson, string? configJson);

        [DllImport("__Internal")]
        public static extern int RtcAddIceCandidate(int nativeHandle, string candidateJson);

        [DllImport("__Internal")]
        public static extern void RtcClose(int nativeHandle);

        [DllImport("__Internal")]
        public static extern void RtcSend(int nativeHandle, byte[] bytes, int size);
    }
}