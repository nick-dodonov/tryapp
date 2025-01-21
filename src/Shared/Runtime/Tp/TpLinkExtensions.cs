using System;
using System.Buffers;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Shared.Tp
{
    public static class TpLinkExtensions
    {
        private unsafe struct RawSpan
        {
            public byte* Ptr;
            public int Length;
        }

        /// <summary>
        /// Helper to write ReadOnlySpan without C#13 (because callback cannot use ref struct in C#9)
        /// </summary>
        public static unsafe void Send(this ITpLink link, ReadOnlySpan<byte> span)
        {
            fixed (byte* ptr = span)
            {
                link.Send(static (writer, state) =>
                {
                    var span = new ReadOnlySpan<byte>(state.Ptr, state.Length);
                    writer.Write(span);
                }, new RawSpan { Ptr = ptr, Length = span.Length });
            }
        }

        /// <summary>
        /// TODO: CommunityToolkit.HighPerformance IBufferWriter can be used instead
        /// </summary>
        public static void Write(this IBufferWriter<byte> writer, byte value)
        {
            var span = writer.GetSpan(1);
            // if (span.Length < 1)
            //     ThrowArgumentExceptionForEndOfBuffer(span.Length, length);
            span[0] = value;
            writer.Advance(1);
        }

        public static unsafe void Write<T>(this IBufferWriter<byte> writer, T value)
            where T : unmanaged
        {
            var length = sizeof(T);
            var span = writer.GetSpan(length);
            // if (span.Length < length)
            //     ThrowArgumentExceptionForEndOfBuffer(span.Length, length);
            ref var r0 = ref MemoryMarshal.GetReference(span);
            Unsafe.WriteUnaligned(ref r0, value);
            writer.Advance(length);
        }

        // ReSharper disable once UnusedMember.Local
        private static void ThrowArgumentExceptionForEndOfBuffer(int spanLength, int length)
            => throw new InternalBufferOverflowException(
                $"Buffer writer can't contain the requested input data ({spanLength} < {length})");
    }
}