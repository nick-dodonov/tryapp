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
            var trendPassedCount = (int)(trendMean * passedDelta / TrendMultiplier);

            var desireDelta = _updateCount - _currentCount;
            var deltaDiff = (int)(desireDelta - passedDelta);

            var fitSigmas = _deltaDiffSet.FitSigmas(deltaDiff, 3);
            _deltaDiffSet.Add(deltaDiff);
            if (fitSigmas)
            {
                var currentDelta = trendPassedCount;

                // anyway smoothly approach to desireCount
                var smoothDiff = 0;
                smoothDiff += deltaDiff / 10;

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

                _currentCount += currentDelta + smoothDiff;
                return UpdateResult.Passed;
            }

            _currentCount += desireDelta;
            return UpdateResult.Source;
        }
    }
}