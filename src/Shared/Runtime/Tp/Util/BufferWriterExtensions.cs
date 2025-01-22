using System.Buffers;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Shared.Tp.Util
{
    /// <summary>
    /// TODO: think to use CommunityToolkit.HighPerformance IBufferWriter extension instead
    /// TODO: add little/big endian writes (prefer little endian to use memcpy instead of shuffle on most client platforms)
    /// TODO: add SequenceReader extensions too
    /// </summary>
    public static class BufferWriterExtensions
    {
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