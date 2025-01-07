using System;
using System.Runtime.InteropServices;
using AOT;
using Shared.Log;

namespace Diagnostics
{
    /// <summary>
    /// TODO: add debug console methods for testing
    /// </summary>
    public static class TryBind
    {
        [DllImport("__Internal")]
        private static extern void SetupTestCallbackString(string message, Action<string> action);

        [DllImport("__Internal")]
        private static extern void SetupTestCallbackBytes(byte[] bytes, int size, Action<byte[], int> action);

        public static void TestCallbacks()
        {
            Slog.Info("WebglRtcApi: TestCallbacks");
            SetupTestCallbackString("test-string", TestCallbackString);

            var bytes = new byte[] { 1, 2, 3, 4, 5 };
            SetupTestCallbackBytes(bytes, bytes.Length, TestCallbackBytes);
            for (var i = 0; i < bytes.Length; ++ i)
                bytes[i] += 10;
        }
        
        [MonoPInvokeCallback(typeof(Action<string>))]
        public static void TestCallbackString(string message) 
            => Slog.Info($"WebglRtcApi: TestCallbackString: \"{message}\"");

        [MonoPInvokeCallback(typeof(Action<byte[]>))]
        public static void TestCallbackBytes(
            [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.U1, SizeParamIndex = 1)]
            byte[] bytes, int length) =>
            Slog.Info($"WebglRtcApi: TestCallbackBytes: [{string.Join(',', bytes)}]");
    }
}