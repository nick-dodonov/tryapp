using System;
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
            var testFramesCount = 1000;
            var idealDelta = TestClock.CountPerSec / 60;
            const int maxDeltaDeviation = 100; //idealDelta / 10;

            var testClock = new TestClock(10000);
            var relaxClock = new RelaxClock<TestPeriod, TestClock>(testClock);
            var prevRelaxTick = relaxClock.Count;
            do
            {
                var fickleDelta = GetFluctuatingValue(idealDelta, maxDeltaDeviation);
                testClock.Add(fickleDelta);

                var updateResult = relaxClock.UpdateFrame(idealDelta);
                
                var relaxTick = relaxClock.Count;
                var relaxDelta = relaxTick - prevRelaxTick;

                Slog.Info($"{updateResult,6}({relaxDelta}) fickle={fickleDelta} set={relaxClock.DeltaDiffCount,2}:{relaxClock.DeltaDiffMean:F1} ± {relaxClock.DeltaDiffStdDeviation:F1}");

                //Assert.That(relaxDelta, Is.InRange(idealDelta - maxDeltaDeviation, idealDelta + maxDeltaDeviation));

                prevRelaxTick = relaxTick;
            } while (--testFramesCount > 0);
        }

        private static readonly Random _random = new Random();
        private static long GetFluctuatingValue(long idealValue, long maxDeviation)
        {
            return idealValue + _random.Next((int)-maxDeviation, (int)maxDeviation + 1);
        }
    }
}