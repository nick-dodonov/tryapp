namespace Shared.Tp.Tick
{
    public sealed class UserClock<T, TPeriod> : IClock<T, TPeriod> 
        where T : unmanaged 
        where TPeriod : IPeriod, new()
    {
        private T _count;

        public T Count => _count;

        public UserClock() => _count = default;
        public UserClock(T count) => _count = count;

        public void Set(T count) => _count = count;
        public void Add(T count) => _count = NumericHelper.Add(_count, count);
    }
}