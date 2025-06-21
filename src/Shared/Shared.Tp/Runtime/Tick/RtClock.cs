namespace Shared.Tp.Tick
{
    public readonly struct RtClock
    {
        public const long RtPerSec = 10_000;
    }

    public readonly struct RtClock<TSourceClock> : IClock<long>
        where TSourceClock : IClock<long>
    {
        private readonly TSourceClock _sourceClock;
        private readonly long _sourceCountPerSecond; 
        
        public RtClock(TSourceClock sourceClock)
        {
            _sourceClock = sourceClock;
            _sourceCountPerSecond = sourceClock.CountPerSec / RtClock.RtPerSec;
        }

        public long Count => _sourceClock.Count / _sourceCountPerSecond;
        public long CountPerSec => RtClock.RtPerSec;
    }
}