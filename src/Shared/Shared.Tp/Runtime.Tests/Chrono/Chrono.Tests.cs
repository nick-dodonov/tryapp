using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using NUnit.Framework;
using Shared.Log;
using Shared.Tp.Chrono;

// ReSharper disable once CheckNamespace
namespace Shared.Tp.Tests
{
    using RtTick = Tick<long, RtPeriod>;
    using CycledRtTick = Tick<ushort, RtPeriod>;
    using RawTick = Tick<long, TestRawPeriod>;

    using TestClock = UserClock<long, TestRawPeriod>;
    using UnityClock = UserClock<float, UnityPeriod>;

    internal readonly struct TestRawPeriod: IPeriod
    {
        public const long CountPerSec = 1_000_000;
        public long InstanceCountPerSec => CountPerSec;
    }

    internal readonly struct TestSecPeriod: IPeriod
    {
        public const long CountPerSec = 1;
        public long InstanceCountPerSec => CountPerSec;
    }

    internal readonly struct UnityPeriod: IPeriod
    {
        private const long CountPerSec = 1;
        public long InstanceCountPerSec => CountPerSec;
    }
    
    public class Chrono_Tests
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static ushort CalcCycled(long raw)
        {
            var rawTick = new RawTick(raw);
            var rtTick = rawTick.RtTick();
            var cycledRtTick = rtTick.Cycled();
            var cycledRt = cycledRtTick.Count;
            return cycledRt;
        }

        [Test]
        public void Tick_Print()
        {
            const long initRaw = 17 * TestRawPeriod.CountPerSec;
            for (var i = 0; i < 5; ++i)
            {
                var raw = initRaw + i;
                var cycledRt = CalcCycled(raw);
                Slog.Info($"Tick_Print: {raw} - {cycledRt}");
            }
        }

        [Test]
        public unsafe void Tick_Cast()
        {
            Slog.Info($"sizeof(RtTick) = {sizeof(RtTick)})");
            Slog.Info($"sizeof(CycledRtTick) = {sizeof(CycledRtTick)})");

            const int testSeconds = 123;
            var testRawTick = new Tick<long, TestRawPeriod>(testSeconds * TestRawPeriod.CountPerSec);
            var testSecTick = new Tick<long, TestSecPeriod>(testSeconds * TestSecPeriod.CountPerSec);
            var rtRawTick = testRawTick.RtTick();
            var rtSecTick = testSecTick.RtTick();
            Assert.AreEqual(testSeconds, rtRawTick.Count / RtPeriod.CountPerSec);
            Assert.AreEqual(testSeconds, rtSecTick.Count / RtPeriod.CountPerSec);

            const ushort testCycledRt = 321;
            var testRtTick = new RtTick((ushort.MaxValue + 1) + testCycledRt);
            var testCycledRtTick = testRtTick.Cycled();
            Assert.AreEqual(testCycledRt, testCycledRtTick.Count);
        }

        [Test]
        public async Task RawClock_Count()
        {
            var clock = new RawClock();

            var start = clock.Count;
            Assert.AreNotEqual(0, start);
            var countPerMs = RawClock.CountPerSec / 1000;
            Assert.AreNotEqual(0, countPerMs);

            await Task.Delay(100);

            var passedCount = clock.Count - start;
            Assert.GreaterOrEqual(passedCount, 100 * countPerMs);
        }

        [Test]
        public void UserClock_Count()
        {
            var clock = new TestClock();
            Assert.AreEqual(0, clock.Count);
            clock.Add(100);
            Assert.AreEqual(100, clock.Count);
            clock.Add(100);
            Assert.AreEqual(200, clock.Count);
            clock.Set(222);
            Assert.AreEqual(222, clock.Count);
        }

        [Test]
        public void DeltaClock_Count()
        {
            var rawClock = new TestClock(1000);
            rawClock.Set(333);
            var deltaClock = new DeltaClock<long, TestRawPeriod, TestClock>(rawClock);
            Assert.AreEqual(0, deltaClock.Count);
            rawClock.Add(100);
            Assert.AreEqual(100, deltaClock.Count);
        }

        [Test]
        public void UnityClock_Count()
        {
            var unityClock = new UnityClock();
            unityClock.Set(11.22f);
            var deltaClock = new DeltaClock<float, UnityPeriod, UnityClock>(unityClock);
            Assert.AreEqual(0.0f, deltaClock.Count);
            unityClock.Add(33.44f);
            Assert.AreEqual(33.44f, deltaClock.Count);
        }
    }
}