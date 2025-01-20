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
        private byte[][]? _buffers;
        private bool _disconnected;

        public void Add(byte[]? bytes)
        {
            if (_disconnected || bytes == null)
            {
                _disconnected = true;
                return;
            }
            _buffers ??= ArrayPool<byte[]>.Shared.Rent(4);
            var count = _buffers.Length;
            if (count >= _count)
            {
                var buffers = ArrayPool<byte[]>.Shared.Rent(count + 1);
                _buffers.CopyTo(buffers, 0);
                _buffers = buffers;
            }
            _buffers[_count++] = bytes;
        }

        public delegate void FeedAction<in TState>(TState state, byte[]? bytes);
        public void Feed<TState>(FeedAction<TState> action, TState state)
        {
            if (_buffers == null)
                return;

            var idx = 0;
            while (idx < _count)
                action(state, _buffers[idx++]);

            if (_disconnected)
                action(state, null);
            Reset();
        }

        private bool Reset()
        {
            ArrayPool<byte[]>.Shared.Return(_buffers, clearArray: true);
            _buffers = null;
            _count = 0;
            var disconnected = _disconnected;
            _disconnected = false;
            return disconnected;
        }
    }
}