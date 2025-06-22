using System.Runtime.CompilerServices;

namespace Shared.Tp.Chrono
{
    public interface IClock<T, TPeriod> where T
        : unmanaged
        where TPeriod : IPeriod, new()
    {
        public static readonly long CountPerSec = new TPeriod().InstanceCountPerSec;

        T Count { get; }
    }

    public readonly struct Clock<T, TSourcePeriod, TSourceClock> : IClock<T, TSourcePeriod>
        where T : unmanaged
        where TSourcePeriod : IPeriod, new()
        where TSourceClock : IClock<T, TSourcePeriod>
    {
        public static readonly long CountPerSec = new TSourcePeriod().InstanceCountPerSec;

        private readonly TSourceClock _sourceClock;
        private readonly T _startCount;

        public Clock(TSourceClock sourceClock)
        {
            _sourceClock = sourceClock;
            _startCount = sourceClock.Count;
        }

        public Clock(TSourceClock sourceClock, T sourceOffset)
        {
            _sourceClock = sourceClock;
            _startCount = NumericHelper.Add(_sourceClock.Count, sourceOffset);
        }

        public T StartCount => _startCount;

        public T Count => NumericHelper.Sub(_sourceClock.Count, _startCount);
    }

    public readonly struct Tick<T, TPeriod>
        where T : unmanaged
        where TPeriod : IPeriod, new()
    {
        public static readonly long CountPerSec = new TPeriod().InstanceCountPerSec;

        private readonly T _count;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Tick(T count) => _count = count;

        public T Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _count;
        }
    }

    public static class ClockExtensions
    {
        public static Tick<T, TSourcePeriod> Tick<T, TSourcePeriod, TSourceClock>(this Clock<T, TSourcePeriod, TSourceClock> clock)
            where T : unmanaged
            where TSourcePeriod : IPeriod, new()
            where TSourceClock : IClock<T, TSourcePeriod>
            => new(clock.Count);
    }

    public static class TickExtensions
    {
        public static float Seconds<TPeriod>(this Tick<long, TPeriod> tick)
            where TPeriod : IPeriod, new()
            => tick.Count / (float)Tick<long, TPeriod>.CountPerSec;
        public static float Seconds<TPeriod>(this Tick<ushort, TPeriod> tick)
            where TPeriod : IPeriod, new()
            => tick.Count / (float)Tick<long, TPeriod>.CountPerSec;
    }
}