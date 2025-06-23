namespace Shared.Tp.Chrono
{
    public readonly struct DeltaClock<T, TSourcePeriod, TSourceClock> : IClock<T, TSourcePeriod>
        where T : unmanaged
        where TSourcePeriod : IPeriod, new()
        where TSourceClock : IClock<T, TSourcePeriod>
    {
        public static readonly long CountPerSec = new TSourcePeriod().InstanceCountPerSec;

        private readonly TSourceClock _sourceClock;
        private readonly T _startCount;

        public DeltaClock(TSourceClock sourceClock)
        {
            _sourceClock = sourceClock;
            _startCount = sourceClock.Count;
        }

        public DeltaClock(TSourceClock sourceClock, T startOffset)
        {
            _sourceClock = sourceClock;
            _startCount = NumericHelper.Sub(_sourceClock.Count, startOffset);
        }

        public T StartCount => _startCount;

        public T Count => NumericHelper.Sub(_sourceClock.Count, _startCount);
    }

    public static class ClockExtensions
    {
        public static Tick<T, TSourcePeriod> Tick<T, TSourcePeriod, TSourceClock>(this DeltaClock<T, TSourcePeriod, TSourceClock> clock)
            where T : unmanaged
            where TSourcePeriod : IPeriod, new()
            where TSourceClock : IClock<T, TSourcePeriod>
            => new(clock.Count);
    }
}