using System;
using System.Collections.Generic;

namespace Shared.Tp.St.Sync
{
    /// <summary>
    /// TODO: reimplement with separate free array plus cyclic indices (reuse the same values frequently)
    /// 
    /// </summary>
    public class History<TKey, TValue>
    {
        private (int key, TValue value)[] _array;

        private int _capacity;
        private int _first;
        private int _count;

        private ref (int key, TValue value) FirstItemRef => ref _array[_first];
        private ref (int key, TValue value) LastItemRef => ref _array[(_first + _count - 1) % _capacity];

        public History(int initCapacity)
        {
            _array = new (int key, TValue value)[initCapacity];
            _capacity = initCapacity;
        }

        public int Capacity => _capacity;
        public int Count => _count;

        public int FirstKey => _count > 0 ? FirstItemRef.key : 0;
        public int LastKey => _count > 0 ? LastItemRef.key : 0;

        public ref TValue LastValueRef
        {
            get
            {
                if (_count <= 0)
                    ThrowInvalidOperation("Remote state is not received yet");
                return ref LastItemRef.value;
            }
        }

        public void ClearUntil(int key, IComparer<int>? comparer = null)
        {
            comparer ??= Comparer<int>.Default;
            while (_count > 0 && comparer.Compare(FirstItemRef.key, key) < 0)
            {
                _first = ++_first % _capacity; 
                --_count;
            }
        }

        public ref TValue AddValueRef(int key)
        {
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
            ref var lastItemRef = ref LastItemRef;
            lastItemRef.key = key;
            return ref lastItemRef.value;
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
                => ref _history._array[(_history._first + _history._count - _iterated) % _history._capacity].value;

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

            public ref (int key, TValue value) Current 
                => ref _history._array[(_history._first + _history._count - _iterated) % _history._capacity];

            public bool MoveNext() => _iterated++ < _history._count;
        }

        public ReverseRefItemEnumerator ReverseRefItems => new(this);
        
        private static void ThrowInvalidOperation(string message) =>
            throw new InvalidOperationException(message);
    }
}