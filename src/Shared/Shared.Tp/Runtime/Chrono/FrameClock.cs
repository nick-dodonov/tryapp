using Shared.Tp.Util.Stat;

namespace Shared.Tp.Chrono
{
    public struct FrameClock<TSourcePeriod, TSourceClock> : IClock<long, TSourcePeriod>
        where TSourcePeriod : IPeriod, new()
        where TSourceClock : IClock<long, TSourcePeriod>
    {
        //private static readonly long CountPerSec = new TSourcePeriod().InstanceCountPerSec;
            
        private readonly TSourceClock _sourceClock;
        private long _currentCount;
        private CycleSampleSet _deltaDiffSet;

        public long Count => _currentCount;

        public FrameClock(TSourceClock sourceClock)
        {
            _sourceClock = sourceClock;
            _currentCount = sourceClock.Count;
            _deltaDiffSet = new();
        }

        public void UpdateFrame(long realDelta)
        {
            var desireCount = _sourceClock.Count;
            var desireDelta = desireCount - _currentCount;

            var deltaDiff = (int)(desireDelta - realDelta);
            if (_deltaDiffSet.Check(deltaDiff))
            {
                _deltaDiffSet.Add(deltaDiff);
                _currentCount += realDelta;
            }
            else
            {
                _currentCount += desireDelta;
            }
        }
    }
}