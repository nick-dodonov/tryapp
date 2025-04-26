using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Shared.Tp.Obj;

namespace Shared.Tp.Util
{
    /// <summary>
    /// TODO: think to use CommunityToolkit.HighPerformance IBufferWriter extension instead
    /// TODO: add little/big endian writes (prefer little endian to use memcpy instead of shuffle on most client platforms)
    /// TODO: add SequenceReader extensions too
    /// 
    /// </summary>
    public static class BufferWriterExtensions
    {
        /// <summary>
        /// TODO: CommunityToolkit.HighPerformance IBufferWriter can be used instead
        /// 
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Write(this IBufferWriter<byte> writer, byte value)
        {
            var span = writer.GetSpan(1);
            // if (span.Length < 1)
            //     ThrowArgumentExceptionForEndOfBuffer(span.Length, length);
            span[0] = value;
            writer.Advance(1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

        // private static void ThrowArgumentExceptionForEndOfBuffer(int spanLength, int length)
        //     => throw new System.IO.InternalBufferOverflowException(
        //         $"Buffer writer can't contain the requested input data ({spanLength} < {length})");

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void PrependSizeWrite(this IBufferWriter<byte> writer, IOwnWriter ownWriter)
        {
            using var prependWriter = PrependBufferWriter.Rent(writer, sizeof(short));
            ownWriter.Serialize(prependWriter);

            //TODO: use SpanWriter similar to SpanReader
            ref var r0 = ref MemoryMarshal.GetReference(prependWriter.ReservedSpan);
            Unsafe.WriteUnaligned(ref r0, (short)prependWriter.WrittenCount);
        }
    }
}