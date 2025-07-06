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
            var prevRelaxTick = relaxClock.Count;

            var idealDelta = TestClock.CountPerSec / 60;
            const float fickleTrend = 1.5f;
            foreach (var (passedDelta, fickleDelta) in EmulateFrames(idealDelta, fickleTrend, TestClock.CountPerSec / 1000))
            {
                testClock.Add(fickleDelta);

                var diff = (testClock.Count - relaxClock.Count - passedDelta); 
                var updateResult = relaxClock.UpdateFrame((int)passedDelta);

                var relaxTick = relaxClock.Count;
                var relaxDelta = relaxTick - prevRelaxTick;

                var relaxDiffResult = relaxClock.DeltaDiffResult;
                var relaxTrendResult = relaxClock.DeltaTrendResult;
                Slog.Info($"{passedDelta}*{fickleTrend:0.0}{fickleDelta-passedDelta*fickleTrend,5:+0;-0} → {updateResult,6}({relaxDelta-passedDelta,5:+0;-0}) ← ({diff,6:+0;-0} : [{relaxDiffResult.Count,2}] {relaxDiffResult.Mean,7:+0.0;-0.0} ± {relaxDiffResult.StdDeviation,6:0.0} | {relaxTrendResult.Mean,4:0.00} ± {relaxTrendResult.StdDeviation,4:0.00})" , 
                    // ReSharper disable once ExplicitCallerInfoArgument
                    string.Empty, string.Empty);

                prevRelaxTick = relaxTick;
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

        private static readonly Random _random = new(10);
        private static long GetFluctuatingValue(long idealValue, long maxDeviation)
        {
            return idealValue + _random.Next((int)-maxDeviation, (int)maxDeviation + 1);
        }
    }
}