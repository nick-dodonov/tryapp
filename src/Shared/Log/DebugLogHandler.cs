#if UNITY_5_6_OR_NEWER
#nullable disable
using System;
using System.Runtime.CompilerServices;
using UnityEngine.Bindings;

// ReSharper disable once CheckNamespace
namespace UnityEngine
{
  internal sealed class DebugLogHandler
  {
    internal static unsafe void Internal_Log(LogType level, LogOption options, string msg)
    {
      var readOnlySpan = msg.AsSpan();
      fixed (char* begin = &readOnlySpan.GetPinnableReference())
      {
        var managedSpanWrapper = new ManagedSpanWrapper(begin, readOnlySpan.Length);
        Internal_Log_Injected(level, options, ref managedSpanWrapper, IntPtr.Zero);
      }
    }

    [MethodImpl(MethodImplOptions.InternalCall)]
    private static extern void Internal_Log_Injected(
      LogType level,
      LogOption options,
      ref ManagedSpanWrapper msg,
      IntPtr obj);
  }
}
#endif