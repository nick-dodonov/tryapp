namespace Shared.Tp.St.Sync
{
    public class StHistory<T> : History<StKey, T>
    {
        public StHistory(int initCapacity) : base(initCapacity) { }

        public delegate void BoundsVisitor(StKey key, ref Item from, ref Item to);

        public void VisitExistingBounds(StKey key, BoundsVisitor visitor)
        {
            var enumerator = ReverseRefItems;
            if (!enumerator.MoveNext())
                return; // no items

            while (true)
            {
                ref var to = ref enumerator.Current;
                if (key.CompareTo(to.Key) <= 0 && enumerator.MoveNext())
                {
                    ref var from = ref enumerator.Current;
                    if (key.CompareTo(from.Key) < 0)
                        continue;

                    visitor(key, ref from, ref to);
                    return;
                }

                visitor(key, ref to, ref to);
                return;
            }
        }
    }
}