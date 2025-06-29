using System.Runtime.CompilerServices;
using NUnit.Framework;
using Shared.Log;
using Shared.Tp.Chrono;

namespace Shared.Tp.Tests.Chrono
{
    using RtTick = Tick<long, RtPeriod>;
    using CycledRtTick = Tick<ushort, RtPeriod>;

    using TestTick = Tick<long, TestPeriod>;
    using SecTick = Tick<long, SecPeriod>;

    public class Tick_Tests
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static ushort CalcCycled(long testCount)
        {
            var testTick = new TestTick(testCount);
            var rtTick = testTick.RtTick();
            var cycledRtTick = rtTick.Cycled();
            var cycledRt = cycledRtTick.Count;
            return cycledRt;
        }

        [Test]
        public void Tick_Print()
        {
            var tick = new TestTick(12345);
            Slog.Info($"TestTick: {tick.Count}");

            const long initTestCount = 17 * TestPeriod.CountPerSec;
            for (var i = 0; i < 5; ++i)
            {
                var testCount = initTestCount + i;
                var cycledRt = CalcCycled(testCount);
                Slog.Info($"Tick_Print: {testCount} - {cycledRt}");
            }
        }

        [Test]
        public unsafe void Tick_Cast()
        {
            Slog.Info($"sizeof(RtTick) = {sizeof(RtTick)})");
            Slog.Info($"sizeof(CycledRtTick) = {sizeof(CycledRtTick)})");

            const int testSeconds = 123;
            var testTick = new TestTick(testSeconds * TestPeriod.CountPerSec);
            var secTick = new SecTick(testSeconds * SecPeriod.CountPerSec);
            var rtTestTick = testTick.RtTick();
            var rtSecTick = secTick.RtTick();
            Assert.AreEqual(testSeconds, rtTestTick.Count / RtPeriod.CountPerSec);
            Assert.AreEqual(testSeconds, rtSecTick.Count / RtPeriod.CountPerSec);

            const ushort testCycledRt = 321;
            var testRtTick = new RtTick((ushort.MaxValue + 1) + testCycledRt);
            var testCycledRtTick = testRtTick.Cycled();
            Assert.AreEqual(testCycledRt, testCycledRtTick.Count);
        }
    }
}