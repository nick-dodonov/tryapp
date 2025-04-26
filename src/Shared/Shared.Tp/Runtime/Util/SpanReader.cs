using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Shared.Tp.Util
{
    public static class SpanReader
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Read<T>(ReadOnlySpan<byte> span)
            where T : unmanaged
        {
            ref var r0 = ref MemoryMarshal.GetReference(span);
            return Unsafe.ReadUnaligned<T>(ref r0);
        }
    }
}