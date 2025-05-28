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
    }
}