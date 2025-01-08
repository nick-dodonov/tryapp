#if UNITY_5_6_OR_NEWER
using System;
using System.Runtime.CompilerServices;

#nullable disable

// ReSharper disable once CheckNamespace
// ReSharper disable InconsistentNaming
namespace UnityEngine.Bindings
{
    /// <summary>
    /// Modified copy of internal UnityEngine implementation
    ///     helps with gc-free logging usage
    /// </summary>
    internal readonly unsafe ref struct ManagedSpanWrapper
    {
        public readonly void* begin;
        public readonly int length;

        public ManagedSpanWrapper(void* begin, int length)
        {
            this.begin = begin;
            this.length = length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Span<T> ToSpan<T>(ManagedSpanWrapper spanWrapper)
        {
            return new(spanWrapper.begin, spanWrapper.length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ReadOnlySpan<T> ToReadOnlySpan<T>(ManagedSpanWrapper spanWrapper)
        {
            return new(spanWrapper.begin, spanWrapper.length);
        }
    }
}
#endif