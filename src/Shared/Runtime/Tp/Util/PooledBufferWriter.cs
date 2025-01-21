using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace Shared.Tp.Util
{
    public class PooledBufferWriter : IBufferWriter<byte>, IDisposable
    {
        private const int MinimumBufferSize = 256;

        private static readonly ArrayPool<byte> _bufferPool = ArrayPool<byte>.Shared;
        private static readonly ConcurrentBag<PooledBufferWriter> _writers = new();

        public static PooledBufferWriter Rent(int initialCapacity = MinimumBufferSize)
        {
            if (!_writers.TryTake(out var writer))
                return new(initialCapacity);

            var moreCapacity = initialCapacity - writer._buffer.Length;
            if (moreCapacity > 0)
                writer.CheckAndResizeBuffer(moreCapacity);

            return writer;
        }

        private static void InternalReturn(PooledBufferWriter writer) => _writers.Add(writer);

        private byte[] _buffer;
        private int _index;

        private PooledBufferWriter(int initialCapacity)
        {
            if (initialCapacity <= 0)
                throw new ArgumentOutOfRangeException(nameof(initialCapacity));
            _buffer = _bufferPool.Rent(initialCapacity);
            _index = 0;
        }

        public ReadOnlyMemory<byte> WrittenMemory => _buffer.AsMemory(0, _index);
        public ReadOnlySpan<byte> WrittenSpan => _buffer.AsSpan(0, _index);

        public int WrittenCount => _index;
        public int Capacity => _buffer.Length;
        public int FreeCapacity => _buffer.Length - _index;

        public void Clear() => ClearHelper();

        private void ClearHelper()
        {
            _buffer.AsSpan(0, _index).Clear();
            _index = 0;
        }

        public void Dispose()
        {
            ClearHelper(); //TODO: speedup: clear only for debug and secured buffers places 
            InternalReturn(this);
        }

        public void Advance(int count)
        {
            if (count < 0 || _index > _buffer.Length - count)
                ThrowAdvanceInvalidOperationException(count, _index, _buffer.Length);

            _index += count;
        }

        public Memory<byte> GetMemory(int sizeHint = 0)
        {
            CheckAndResizeBuffer(sizeHint);
            return _buffer.AsMemory(_index);
        }

        public Span<byte> GetSpan(int sizeHint = 0)
        {
            CheckAndResizeBuffer(sizeHint);
            return _buffer.AsSpan(_index);
        }

        private void CheckAndResizeBuffer(int sizeHint)
        {
            if (sizeHint < 0)
                throw new ArgumentOutOfRangeException(nameof(sizeHint));

            if (sizeHint == 0)
                sizeHint = MinimumBufferSize;

            var availableSpace = _buffer.Length - _index;
            if (sizeHint > availableSpace)
            {
                var growBy = Math.Max(sizeHint, _buffer.Length);
                var newSize = checked(_buffer.Length + growBy);
                var oldBuffer = _buffer;

                _buffer = _bufferPool.Rent(newSize);

                Debug.Assert(oldBuffer.Length >= _index);
                Debug.Assert(_buffer.Length >= _index);

                var previousBuffer = oldBuffer.AsSpan(0, _index);
                previousBuffer.CopyTo(_buffer);
                previousBuffer.Clear();

                _bufferPool.Return(oldBuffer);
            }

            Debug.Assert(_buffer.Length - _index > 0);
            Debug.Assert(_buffer.Length - _index >= sizeHint);
        }

        private static void ThrowAdvanceInvalidOperationException(int advance, int index, int capacity) =>
            throw new InvalidOperationException($"Advance out of bounds: advance={advance} index={index} capacity={capacity}");
    }
}