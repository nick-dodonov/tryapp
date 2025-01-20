using System;
using System.Buffers;

namespace Shared.Tp
{
    /// <summary>
    /// Helper to temporary keep send/received bytes messages.
    /// Allows to handle edge cases of connection establishment when
    ///     link implementation, custom logic or extension layer didn't set up callbacks yet
    /// 
    /// TODO: thread-safe with CAS interlocked (it isn't required now because of known nature of current impls) 
    /// </summary>
    internal struct PostponedBytes
    {
        private int _count;
        private int[]? _lengths;
        private int _total;
        private byte[]? _buffer;

        public bool Disconnected { get; private set; }
        public void Disconnect() => Disconnected = true;

        public void Add(ReadOnlySpan<byte> span)
        {
            if (Disconnected)
                return;

            if (_lengths == null)
            {
                _lengths = ArrayPool<int>.Shared.Rent(4);
                _buffer = ArrayPool<byte>.Shared.Rent(1024);
            }
            else if (_count >= _lengths.Length)
            {
                var lengths = ArrayPool<int>.Shared.Rent(_lengths.Length + 1);
                _lengths.CopyTo(lengths, 0);
                var temp = _lengths;
                _lengths = lengths;
                ArrayPool<int>.Shared.Return(temp);
            }

            var length = span.Length;
            var next = _total + length;
            if (next > _buffer!.Length)
            {
                var buffer = ArrayPool<byte>.Shared.Rent(next);
                _buffer.CopyTo(buffer, 0);
                var temp = _buffer;
                _buffer = buffer;
                ArrayPool<byte>.Shared.Return(temp);
            }

            span.CopyTo(_buffer.AsSpan(_total));
            _total += length;
            _lengths[_count++] = length;
        }

        public delegate void FeedAction<in TState>(TState state, ReadOnlySpan<byte> span);
        public void Feed<TState>(FeedAction<TState> action, TState state)
        {
            if (_lengths == null)
                return;

            var idx = 0;
            var total = 0;
            while (idx < _count)
            {
                var length = _lengths[idx++];
                action(state, _buffer.AsSpan(total, length));
                total += length;
            }

            if (Disconnected)
                action(state, null);

            Reset();
        }

        private void Reset()
        {
            ArrayPool<byte>.Shared.Return(_buffer, clearArray: true);
            _buffer = null;
            _total = 0;

            ArrayPool<int>.Shared.Return(_lengths, clearArray: true);
            _lengths = null;
            _count = 0;

            Disconnected = false;
        }
    }
}