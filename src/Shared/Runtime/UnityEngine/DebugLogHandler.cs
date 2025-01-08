#if UNITY_5_6_OR_NEWER
#nullable disable
using System;
using System.Runtime.CompilerServices;
using UnityEngine.Bindings;

// ReSharper disable once CheckNamespace
namespace UnityEngine
{
    /// <summary>
    /// Modified copy of internal UnityEngine implementation
    ///     helps with gc-free logging usage
    /// </summary>
    internal sealed class DebugLogHandler
    {
        // [MethodImpl(MethodImplOptions.AggressiveInlining)]
        // internal static void Internal_Log(LogType level, LogOption options, string msg) 
        //     => Internal_Log(level, options, msg.AsSpan());

        [HideInCallstack, MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static unsafe void Internal_Log(LogType level, LogOption options, ReadOnlySpan<char> msg)
        {
            fixed (char* begin = msg) //&msg.GetPinnableReference())
            {
                var managedSpanWrapper = new ManagedSpanWrapper(begin, msg.Length);
                Internal_Log_Injected(level, options, ref managedSpanWrapper, IntPtr.Zero);
            }
        }

        [HideInCallstack, MethodImpl(MethodImplOptions.InternalCall)]
        private static extern void Internal_Log_Injected(
            LogType level,
            LogOption options,
            ref ManagedSpanWrapper msg,
            IntPtr obj);
    }
}
#endif