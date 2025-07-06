using System;
using System.Collections.Generic;
using NUnit.Framework;
using Shared.Log;
using Shared.Tp.Chrono;

namespace Shared.Tp.Tests.Chrono
{
    using TestClock = UserClock<long, TestPeriod>;
    
    public class RelaxClock_Tests
    {
        [Test]
        public void RelaxClock_Update()
        {
            var testClock = new TestClock(10000);
            var relaxClock = new RelaxClock<TestPeriod, TestClock>(testClock);

            var idealDelta = TestClock.CountPerSec / 60;
            const float fickleTrend = 1.5f;
            foreach (var (passedDelta, fickleDelta) in EmulateFrames(idealDelta, fickleTrend, TestClock.CountPerSec / 1000))
            {
                testClock.Add(fickleDelta);
                var updateResult = relaxClock.UpdateFrame((int)passedDelta);

                var relaxDiffResult = relaxClock.DeltaDiffResult;
                var relaxTrendResult = relaxClock.DeltaTrendResult;
                
                var idealTrendPassedDelta = (int)(fickleTrend * passedDelta); // show relatively
                Slog.Info($"{passedDelta}*{fickleTrend:0.0}{fickleDelta-idealTrendPassedDelta,5:+0;-0} → {updateResult,6}({relaxClock.LastDelta-idealTrendPassedDelta,5:+0;-0}) ← ({relaxClock.LastDesireDiff,6:+0;-0} : [{relaxDiffResult.Count,2}] {relaxDiffResult.Mean,7:+0.0;-0.0} ± {relaxDiffResult.StdDeviation,6:0.0} | {relaxTrendResult.Mean,4:0.00} ± {relaxTrendResult.StdDeviation,4:0.00})" , 
                    // ReSharper disable once ExplicitCallerInfoArgument
                    string.Empty, string.Empty);
            }
        }

        private static IEnumerable<(long passedDelta, long fickleDelta)> EmulateFrames(long idealDelta, float fickleTrend, long fickleDeviation)
        {
            const int testFramesCount = 300;
            for (var i = 0; i < testFramesCount; ++i)
            {
                var trendDelta = (long)(fickleTrend * idealDelta);
                var fickleDelta = GetFluctuatingValue(trendDelta, fickleDeviation);
                yield return (idealDelta, fickleDelta);
            }
        }

        private static readonly Random _random = new(3);
        private static long GetFluctuatingValue(long idealValue, long maxDeviation)
        {
            return idealValue + _random.Next((int)-maxDeviation, (int)maxDeviation + 1);
        }
    }
}