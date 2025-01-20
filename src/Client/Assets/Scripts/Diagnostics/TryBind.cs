using System;
using System.Runtime.InteropServices;
using AOT;
using Diagnostics.Debug;
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
        
        //[Preserve, DebugAction]
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
        private static extern void SetupTestCallbackObj(IntPtr ptr, Action<IntPtr> action);
        
        [MonoPInvokeCallback(typeof(Action<IntPtr>))]
        public static void TestCallbackObj(IntPtr ptr)
        {
            Slog.Info($"ptr: {ptr}");
            var handle = GCHandle.FromIntPtr(ptr);
            Slog.Info($"handle: {handle}");
            var obj = (TestObj)handle.Target;
            Slog.Info($"obj: {obj.GetHashCode()}");
            Slog.Info($"obj-content: {obj.Content}");
        }

        //[Preserve, DebugAction]
        public static void TestManagedPin()
        {
            Slog.Info("start");
            var obj = new TestObj { Content = "test text" };
            Slog.Info($"obj-content: {obj.Content}");
            Slog.Info($"obj-hashcode: {obj.GetHashCode()}");
            var handle = GCHandle.Alloc(obj);
            Slog.Info($"handle: {handle}");
            var ptr = GCHandle.ToIntPtr(handle);
            Slog.Info($"ptr: {ptr}");
            SetupTestCallbackObj(ptr, TestCallbackObj);
            Slog.Info("finish");
            
            //TODO: try more possibly more efficient unity specific variant Unity.Collections.LowLevel.Unsafe.UnsafeUtility.PinGCObjectAndGetAddress()
            //  https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Unity.Collections.LowLevel.Unsafe.UnsafeUtility.PinGCArrayAndGetDataAddress.html
        }
    }
}