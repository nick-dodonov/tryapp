using System;

namespace Shared.Tp.St.Sync
{
    /// <summary>
    /// TODO: reimplement with separate free array plus cyclic indices (reuse the same values frequently)
    /// 
    /// </summary>
    public class History<T>
    {
        private (int frame, T value)[] _array;

        private int _capacity;
        private int _first;
        private int _count;

        private ref (int frame, T value) FirstItemRef => ref _array[_first];
        private ref (int frame, T value) LastItemRef => ref _array[(_first + _count - 1) % _capacity];

        public History(int initCapacity)
        {
            _array = new (int frame, T value)[initCapacity];
            _capacity = initCapacity;
        }

        public int Capacity => _capacity;
        public int Count => _count;

        public int FirstFrame => _count > 0 ? FirstItemRef.frame : 0;
        public int LastFrame => _count > 0 ? LastItemRef.frame : 0;

        public ref T LastValueRef
        {
            get
            {
                if (_count <= 0)
                    ThrowInvalidOperation("Remote state is not received yet");
                return ref LastItemRef.value;
            }
        }

        public void ClearUntil(int frame)
        {
            while (_count > 0 && FirstItemRef.frame < frame)
            {
                _first = ++_first % _capacity; 
                --_count;
            }
        }

        public ref T AddValueRef(int frame)
        {
            if (_count >= _capacity)
            {
                _capacity *= 2;
                Array.Resize(ref _array, _capacity);
            }

            ++_count;
            ref var lastItemRef = ref LastItemRef;
            lastItemRef.frame = frame;
            return ref lastItemRef.value;
        }

        private static void ThrowInvalidOperation(string message) =>
            throw new InvalidOperationException(message);
    }
}