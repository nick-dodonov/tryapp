using System.Runtime.CompilerServices;

namespace Shared.Tp.Chrono
{
    using RtTick = Tick<long, RtPeriod>;
    using CycledRtTick = Tick<ushort, RtPeriod>;

    public readonly struct RtPeriod : IPeriod
    {
        public const long CountPerSec = 10_000; // (1/10 ms | 100 mk) run-tick is the selected time accuracy for networking
        public const int CountPerMs = 10;

        public long InstanceCountPerSec => CountPerSec;

        public static readonly long RawPerRt = RawPeriod.CountPerSec / CountPerSec;
    }

    public static class RtExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long RtCount<TPeriod>(this IClock<long, TPeriod> clock)
            where TPeriod : IPeriod, new()
            => PeriodConverter<TPeriod, RtPeriod>.Convert(clock.Count);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static RtTick RtTick<TPeriod>(this Tick<long, TPeriod> tick)
            where TPeriod : IPeriod, new()
            => new(PeriodConverter<TPeriod, RtPeriod>.Convert(tick.Count));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float RtFraction(this Tick<long, RawPeriod> tick)
        {
            var ratio = PeriodConverter<RawPeriod, RtPeriod>.SourceCountDen;
            return tick.Count % ratio / (float)ratio;
        }

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