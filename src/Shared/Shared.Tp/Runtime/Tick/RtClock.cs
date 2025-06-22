namespace Shared.Tp.Tick
{
    using RtTick = Tick<long, RtPeriod>;
    using CycledRtTick = Tick<ushort, RtPeriod>;

    public readonly struct RtPeriod : IPeriod
    {
        public const long CountPerSec = 10_000;
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

        public long Count => PeriodConvert<TSourcePeriod, RtPeriod>.Convert(_sourceClock.Count);
    }

    public static class RtTickExtensions
    {
        public static RtTick ToRt<TPeriod>(this Tick<long, TPeriod> tick)
            where TPeriod : IPeriod, new()
            => new(PeriodConvert<TPeriod, RtPeriod>.Convert(tick.Count));

        public static CycledRtTick ToCycled(this RtTick tick, ushort maxCount = ushort.MaxValue)
            => new((ushort)(tick.Count % (maxCount + 1)));
    }
}