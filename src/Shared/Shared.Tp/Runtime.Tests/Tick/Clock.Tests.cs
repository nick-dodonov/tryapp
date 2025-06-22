using System.Threading.Tasks;
using NUnit.Framework;
using Shared.Log;
using Shared.Tp.Tick;

// ReSharper disable once CheckNamespace
namespace Shared.Tp.Tests
{
    using TestClock = UserClock<long, TestPeriod>;
    
    internal readonly struct TestPeriod: IPeriod
    {
        public long InstanceCountPerSec => 1_000_000;
    }
    
    public class ClockTests
    {
        [Test]
        public unsafe void Tick_Cast()
        {
            var testTick = new Tick<long, TestPeriod>(100);
            var rtTick = testTick.ToRt();
            Slog.Info($"sizeof(RtTick) = {sizeof(Tick<long, RtPeriod>)})");
            Slog.Info($"sizeof(CycledRtTick) = {sizeof(Tick<ushort, RtPeriod>)})");
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
        public void Clock_Count()
        {
            var rawClock = new TestClock(1000);
            rawClock.Set(333);
            var clock = new Clock<long, TestPeriod, UserClock<long, TestPeriod>>(rawClock);
            Assert.AreEqual(0, clock.Count);
            rawClock.Add(100);
            Assert.AreEqual(100, clock.Count);
        }

        // [Test]
        // public void RtClock_Count()
        // {
        //     const long testSeconds = 123;
        //
        //     const long rawCountPerSecond = 10_000_000;
        //     var rawClock = new ManualClock<long>(rawCountPerSecond);
        //
        //     var rtClock = new RtClock<ManualClock<long>>(rawClock);
        //
        //     rawClock.Add(testSeconds * rawCountPerSecond);
        //
        //     var rt = rtClock.Count;
        //     Assert.AreEqual(testSeconds, rt / RtClock.RtPerSec);
        //
        //     // var rtPoint = rtClock.ClockPoint();
        //     // rtPoint.
        // }
    }
}