using System;

namespace Shared.Tp.St.Sync
{
    public static class HistoryExtensions
    {
        public delegate void BoundsVisitor<TKey, TValue>(
            TKey key, 
            ref History<TKey, TValue>.Item from, 
            ref History<TKey, TValue>.Item to)
            where TKey : unmanaged, IComparable<TKey>;

        /// <summary>
        /// Backward history visitor for key bounds. 
        /// </summary>
        public static bool VisitExistingBounds<TKey, TValue>(
            this History<TKey, TValue> history, TKey key, BoundsVisitor<TKey, TValue> visitor)
            where TKey : unmanaged, IComparable<TKey>
        {
            var enumerator = history.ReverseRefItems;
            if (!enumerator.MoveNext())
                return false; // no items

            while (true)
            {
                ref var to = ref enumerator.Current;
                if (key.CompareTo(to.Key) <= 0 && enumerator.MoveNext())
                {
                    ref var from = ref enumerator.Current;
                    if (key.CompareTo(from.Key) < 0)
                        continue;

                    visitor(key, ref from, ref to);
                    return true;
                }

                visitor(key, ref to, ref to);
                return true;
            }
        }

        public static bool InCycleBounds(short key, short fromKey, short toKey)
        {
            throw new NotImplementedException();
        }

        public static bool InCycleBounds<TKey>(TKey key, TKey fromKey, TKey toKey)
            where TKey : unmanaged, IComparable<TKey>
        {
            if (fromKey.CompareTo(toKey) <= 0)
                return key.CompareTo(fromKey) >= 0 && key.CompareTo(toKey) <= 0;
            return key.CompareTo(fromKey) >= 0 || key.CompareTo(toKey) <= 0; // toKey is cycled
        }

        /// <summary>
        /// Naive implementation of cycled key bounds visitor.
        /// </summary>
        public static bool VisitCycleKeyBounds<TKey, TValue>(
            this History<TKey, TValue> history, 
            TKey key, BoundsVisitor<TKey, TValue> visitor)
            where TKey : unmanaged, IComparable<TKey>
        {
            var enumerator = history.ReverseRefItems;
            if (!enumerator.MoveNext())
                return false; // no items

            ref var last = ref enumerator.Current;
            if (!InCycleBounds(key, history.UnsafeFirstItemRef.Key, last.Key))
            {
                visitor(key, ref last, ref last);
                return true;
            }

            while (true)
            {
                ref var to = ref enumerator.Current;
                var toKey = to.Key;
                if (enumerator.MoveNext())
                {
                    ref var from = ref enumerator.Current;
                    var fromKey = from.Key;

                    // TODO: optimize as some InBounds if-branches can be skipped as already checked in previous loop iterations
                    if (!InCycleBounds(key, fromKey, toKey))
                        continue;

                    visitor(key, ref from, ref to);
                    return true;
                }

                visitor(key, ref to, ref to);
                return true;
            }
        }
    }
}