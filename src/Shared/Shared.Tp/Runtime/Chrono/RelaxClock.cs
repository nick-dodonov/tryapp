using System.Diagnostics;
using Shared.Tp.Util.Stat;

namespace Shared.Tp.Chrono
{
    public struct RelaxClock<TSourcePeriod, TSourceClock> : IClock<long, TSourcePeriod>
        where TSourcePeriod : IPeriod, new()
        where TSourceClock : IClock<long, TSourcePeriod>
    {
        //private static readonly long CountPerSec = new TSourcePeriod().InstanceCountPerSec;
            
        private readonly TSourceClock _sourceClock;

        private long _updateCount;
        private long _currentCount;
        public long Count => _currentCount;
        
        private long _lastDelta;
        public long LastDelta => _lastDelta;

        private int _lastDesireDiff;
        public int LastDesireDiff => _lastDesireDiff;
        
        private CycleSampleSet _deltaDiffSet;
        public CycleSampleSet.ResultData DeltaDiffResult => _deltaDiffSet.Result;

        private const long TrendMultiplier = 10000;
        private CycleSampleSet _deltaTrendSet;
        public CycleSampleSet.ResultData DeltaTrendResult
        {
            get
            {
                var result = _deltaTrendSet.Result;
                return new()
                {
                    Count = result.Count,
                    Mean = result.Mean / TrendMultiplier,
                    StdDeviation = result.StdDeviation / TrendMultiplier
                };
            }
        }

        public RelaxClock(TSourceClock sourceClock)
        {
            _sourceClock = sourceClock;
            _updateCount = _currentCount = sourceClock.Count;
            _lastDelta = 0;
            _lastDesireDiff = 0;
            _deltaDiffSet = new();
            _deltaTrendSet = new();
        }

        public enum UpdateResult
        {
            Passed,
            Source,
        }

        public UpdateResult UpdateFrame(int passedDelta)
        {
            Debug.Assert(passedDelta > 0);

            var prevCount = _updateCount;
            _updateCount = _sourceClock.Count;

            { // delta trend calculation
                var sourceDelta = (int)(_updateCount - prevCount);
                Debug.Assert(sourceDelta > 0);

                var deltaTrend = TrendMultiplier * sourceDelta / passedDelta;
                _deltaTrendSet.Add((int)deltaTrend);
            }

            var trendMean = _deltaTrendSet.Mean;
            var trendPassedDelta = (int)(trendMean * passedDelta / TrendMultiplier);

            var desireDelta = _updateCount - _currentCount;
            _lastDesireDiff = (int)(desireDelta - trendPassedDelta);

            var fitSigmas = _deltaDiffSet.FitSigmas(_lastDesireDiff, 3);
            _deltaDiffSet.Add(_lastDesireDiff);
            if (fitSigmas)
            {
                var currentDelta = trendPassedDelta;

                // anyway smoothly approach to desireCount
                var smoothDiff = 0;
                smoothDiff += _lastDesireDiff / 10;

                // var fitDiff = _deltaDiffSet.StdDeviation / 10;
                // if (smoothDiff < -fitDiff)
                //
                // if (deltaDiff < -fitDiff)
                //     smoothDiff = deltaDiff;
                // if (smoothDiff < -minDiff || 
                //     smoothDiff > minDiff)
                //     smoothDiff /= 10;
                //
                // currentDelta += smoothDiff;
                // if (currentDelta <= 0) // never go back 
                //     currentDelta = 1;

                _lastDelta = currentDelta + smoothDiff;
                _currentCount += _lastDelta;
                return UpdateResult.Passed;
            }

            _lastDelta = desireDelta;
            _currentCount += _lastDelta;
            return UpdateResult.Source;
        }
    }
}