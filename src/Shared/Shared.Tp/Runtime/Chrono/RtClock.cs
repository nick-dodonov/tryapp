using System.Runtime.CompilerServices;

namespace Shared.Tp.Chrono
{
    using RtTick = Tick<long, RtPeriod>;
    using CycledRtTick = Tick<ushort, RtPeriod>;

    public readonly struct RtPeriod : IPeriod
    {
        public const long CountPerSec = 10_000; // (1/10 ms | 100 mk) run-tick is the selected time accuracy for networking
        public long InstanceCountPerSec => CountPerSec;
    }

    public readonly struct RtClock<TSourcePeriod, TSourceClock> : IClock<long, RtPeriod>
        where TSourcePeriod : IPeriod, new()
        where TSourceClock : IClock<long, TSourcePeriod> 
    {
        private readonly TSourceClock _sourceClock;
        
        public RtClock(TSourceClock sourceClock)
        {
            _sourceClock = sourceClock;
        }

        public long Count => PeriodConverter<TSourcePeriod, RtPeriod>.Convert(_sourceClock.Count);
    }

    public static class RtExtensions
    {
        public static long RtCount<TPeriod>(this IClock<long, TPeriod> clock)
            where TPeriod : IPeriod, new()
            => PeriodConverter<TPeriod, RtPeriod>.Convert(clock.Count);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static RtTick RtTick<TPeriod>(this Tick<long, TPeriod> tick)
            where TPeriod : IPeriod, new()
            => new(PeriodConverter<TPeriod, RtPeriod>.Convert(tick.Count));

        /// <summary>
        /// CycledRtTick is a "packed" absolute time value. Used to synchronize remote sides.
        /// Events to associate cannot have a time interval more than max value (re-sync happens in this case).
        /// 
        /// With Rt as 1/10 ms:
        /// - 0xFFFF ~6.5 sec
        /// - 0xF_FFFF ~105 sec
        /// - 0xFF_FFFF ~27 min
        /// - 0xFFF_FFFF ~7.5 hours
        /// - 0xFFFF_FFFF ~5 days
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static CycledRtTick Cycled(this RtTick tick, ushort maxCount = ushort.MaxValue)
            => new((ushort)(tick.Count % (maxCount + 1)));
    }
}