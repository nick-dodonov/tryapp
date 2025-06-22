namespace Shared.Tp.Tick
{
    using RtTick = Tick<long, RtPeriod>;
    using CycledRtTick = Tick<ushort, RtPeriod>;

    public readonly struct RtPeriod : IPeriod
    {
        public const long CountPerSec = 10_000;
        public long InstanceCountPerSec => CountPerSec;
    }

    public readonly struct RtClock<TPeriod, TSourceClock> : IClock<long, RtPeriod>
        where TPeriod : IPeriod, new()
        where TSourceClock : IClock<long, TPeriod> 
    {
        private readonly TSourceClock _sourceClock;
        private readonly long _sourceCountRate; 
        
        public RtClock(TSourceClock sourceClock)
        {
            _sourceClock = sourceClock;
            _sourceCountRate = new TPeriod().InstanceCountPerSec / RtPeriod.CountPerSec;
        }

        public long Count => _sourceClock.Count / _sourceCountRate;
    }

    public static class RtTickExtensions
    {
        public static RtTick ToRt<TPeriod>(this Tick<long, TPeriod> tick) 
            where TPeriod : IPeriod, new() 
            => new(tick.Count / (new TPeriod().InstanceCountPerSec / RtPeriod.CountPerSec));

        public static CycledRtTick ToCycledRt<TPeriod>(this Tick<long, TPeriod> tick, ushort maxCount = ushort.MaxValue)
            where TPeriod : IPeriod, new() 
            => new((ushort)(tick.Count % (maxCount + 1)));
        
        // public static float ToSeconds(this Tick<long, RtClock> point) => (float)((double)point.Count / RtPeriod.CountPerSec);
        // public static float ToSeconds(this Tick<ushort, RtClock> point) => (float)point.Count / RtPeriod.CountPerSec;
    }
}