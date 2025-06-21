namespace Shared.Tp.Tick
{
    public sealed class ManualClock<T> : IClock<T> where T : unmanaged
    {
        private T _count;

        public ManualClock(long countPerSecond) => CountPerSec = countPerSecond;

        public T Count => _count;
        public long CountPerSec { get; }

        public void Set(T count) => _count = count;
        public void Add(T count) => _count = NumericHelper.Add(_count, count);
    }
}