using System;

namespace Shared.Tp.St.Sync
{
    /// <summary>
    /// TODO: reimplement with separate free array plus cyclic indices (reuse the same values frequently)
    /// 
    /// </summary>
    public class History<TKey, TValue>
        where TKey : unmanaged, IComparable<TKey>
    {
        public struct Item
        {
            public TKey Key;
            public TValue Value;
        }
        private Item[] _array;

        private int _capacity;
        private int _first;
        private int _count;

        private ref Item UnsafeFirstItemRef => ref _array[_first];
        private ref Item UnsafeLastItemRef => ref _array[(_first + _count - 1) % _capacity];

        public History(int initCapacity)
        {
            _array = new Item[initCapacity];
            _capacity = initCapacity;
        }

        public int Capacity => _capacity;
        public int Count => _count;

        public TKey FirstKeyOrDefault => _count > 0 ? UnsafeFirstItemRef.Key : default;
        public TKey LastKeyOrDefault => _count > 0 ? UnsafeLastItemRef.Key : default;

        public ref TValue LastValueRef
        {
            get
            {
                if (_count <= 0)
                    ThrowInvalidOperation("LastValueRef: remote state is not received yet");
                return ref UnsafeLastItemRef.Value;
            }
        }

        public void ClearUntil(TKey key)
        {
            while (_count > 0 && UnsafeFirstItemRef.Key.CompareTo(key) < 0)
            {
                _first = ++_first % _capacity; 
                --_count;
            }
        }

        public ref TValue AddValueRef(TKey key)
        {
            //TODO: assert added key is more than last

            if (_count >= _capacity)
            {
                var capacity = _capacity * 2;
                Array.Resize(ref _array, capacity);

                // copy to the end of the array to handle the case resize on cycle
                var offset = capacity - _capacity;
                Array.Copy(_array, 0, _array, offset, _capacity);

                _capacity = capacity;
                _first += offset;
            }

            ++_count;
            ref var lastItemRef = ref UnsafeLastItemRef;
            lastItemRef.Key = key;
            return ref lastItemRef.Value;
        }

        public struct ReverseRefValueEnumerator
        {
            private readonly History<TKey, TValue> _history;
            private int _iterated;

            internal ReverseRefValueEnumerator(History<TKey, TValue> history)
            {
                _history = history;
                _iterated = 0;
            }

            public ReverseRefValueEnumerator GetEnumerator() => this;

            public ref TValue Current 
                => ref _history._array[(_history._first + _history._count - _iterated) % _history._capacity].Value;

            public bool MoveNext() => _iterated++ < _history._count;
        }

        public ReverseRefValueEnumerator ReverseRefValues => new(this);

        public struct ReverseRefItemEnumerator
        {
            private readonly History<TKey, TValue> _history;
            private int _iterated;

            internal ReverseRefItemEnumerator(History<TKey, TValue> history)
            {
                _history = history;
                _iterated = 0;
            }

            public ReverseRefItemEnumerator GetEnumerator() => this;

            public ref Item Current 
                => ref _history._array[(_history._first + _history._count - _iterated) % _history._capacity];

            public bool MoveNext() => _iterated++ < _history._count;
        }

        public ReverseRefItemEnumerator ReverseRefItems => new(this);
        
        private static void ThrowInvalidOperation(string message) =>
            throw new InvalidOperationException(message);
    }
}