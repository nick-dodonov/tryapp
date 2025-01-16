using System;
using System.Runtime.InteropServices;
using AOT;
using Client.Diagnostics.Debug;
using Shared.Log;
using UnityEngine.Scripting;

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

        [MonoPInvokeCallback(typeof(Action<string>))]
        public static void TestCallbackString(string message) 
            => Slog.Info($"\"{message}\"");

        [MonoPInvokeCallback(typeof(Action<byte[]>))]
        public static void TestCallbackBytes(
            [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.U1, SizeParamIndex = 1)]
            byte[] bytes, int length) =>
            Slog.Info($"[{string.Join(',', bytes)}]");
        
        [Preserve, DebugAction]
        public static void TestCallbacks()
        {
            SetupTestCallbackString("test-string", TestCallbackString);

            var bytes = new byte[] { 1, 2, 3, 4, 5 };
            SetupTestCallbackBytes(bytes, bytes.Length, TestCallbackBytes);
            for (var i = 0; i < bytes.Length; ++ i)
                bytes[i] += 10;
        }

        private class TestObj
        {
            public string Content;
        }
        
        [DllImport("__Internal")]
        private static extern void SetupTestCallbackObj(IntPtr obj, Action<IntPtr> action);
        [MonoPInvokeCallback(typeof(Action<IntPtr>))]
        public static void TestCallbackObj(IntPtr ptr)
        {
            Slog.Info($"ptr: {ptr}");
            var handle = GCHandle.FromIntPtr(ptr);
            var obj = (TestObj)handle.Target;
            Slog.Info($"content: {obj.Content}");
        }

        [Preserve, DebugAction]
        public static void TestManagedPin()
        {
            var obj = new TestObj { Content = "test text" };
            
            var handle = GCHandle.Alloc(obj, GCHandleType.Pinned);
            var ptr = handle.AddrOfPinnedObject();
            SetupTestCallbackObj(ptr, TestCallbackObj);
        }
    }
}