using Shared.Tp.Util.Stat;

namespace Shared.Tp.Chrono
{
    public struct RelaxClock<TSourcePeriod, TSourceClock> : IClock<long, TSourcePeriod>
        where TSourcePeriod : IPeriod, new()
        where TSourceClock : IClock<long, TSourcePeriod>
    {
        private static readonly long CountPerSec = new TSourcePeriod().InstanceCountPerSec;
            
        private readonly TSourceClock _sourceClock;

        private long _currentCount;
        public long Count => _currentCount;

        private CycleSampleSet _deltaDiffSet;
        public int DeltaDiffCount => _deltaDiffSet.Count;
        public float DeltaDiffMean => _deltaDiffSet.Mean;
        public float DeltaDiffStdDeviation => _deltaDiffSet.StdDeviation;

        public RelaxClock(TSourceClock sourceClock)
        {
            _sourceClock = sourceClock;
            _currentCount = sourceClock.Count;
            _deltaDiffSet = new();
        }

        public enum UpdateResult
        {
            Passed,
            Source,
        }

        public UpdateResult UpdateFrame(long passedDelta)
        {
            var desireCount = _sourceClock.Count;
            var desireDelta = desireCount - _currentCount;

            var deltaDiff = (int)(desireDelta - passedDelta);

            var fitSigmas = _deltaDiffSet.FitSigmas(deltaDiff, 3);
            _deltaDiffSet.Add(deltaDiff);
            if (fitSigmas)
            {
                _currentCount += passedDelta;
                return UpdateResult.Passed;
            }

            _currentCount += desireDelta;
            return UpdateResult.Source;
        }
    }
}