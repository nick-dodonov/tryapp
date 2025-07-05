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

            foreach (var (passedDelta, fickleDelta) in EmulateFrames())
            {
                testClock.Add(fickleDelta);
                var updateResult = relaxClock.UpdateFrame(passedDelta);

                var relaxTick = relaxClock.Count;
                var relaxDelta = relaxTick - prevRelaxTick;

                Slog.Info($"{updateResult,6}({relaxDelta}) fickle={fickleDelta} set={relaxClock.DeltaDiffCount,2}:{relaxClock.DeltaDiffMean:F1} ± {relaxClock.DeltaDiffStdDeviation:F1}");
                prevRelaxTick = relaxTick;
            }
        }

        private static IEnumerable<(long passedDelta, long fickleDelta)> EmulateFrames()
        {
            var idealDelta = TestClock.CountPerSec / 60;
            const int maxDeltaDeviation = 100;

            const int testFramesCount = 1000;
            for (var i = 0; i < testFramesCount; ++i)
            {
                var fickleDelta = GetFluctuatingValue(idealDelta, maxDeltaDeviation);
                yield return (idealDelta, fickleDelta);
            }
        }

        private static readonly Random _random = new();
        private static long GetFluctuatingValue(long idealValue, long maxDeviation)
        {
            return idealValue + _random.Next((int)-maxDeviation, (int)maxDeviation + 1);
        }
    }
}