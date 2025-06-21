using System.Threading.Tasks;
using NUnit.Framework;
using Shared.Tp.Tick;

// ReSharper disable once CheckNamespace
namespace Shared.Tp.Tests
{
    public class ClockTests
    {
        [Test]
        public async Task SystemClock_Count()
        {
            var clock = new SystemClock();

            var start = clock.Count;
            Assert.AreNotEqual(0, start);
            var countPerMs = clock.CountPerSec / 1000;
            Assert.AreNotEqual(0, countPerMs);

            await Task.Delay(100);

            var passedCount = clock.Count - start;
            Assert.GreaterOrEqual(passedCount, 100 * countPerMs);
        }

        [Test]
        public void ManualClock_Count()
        {
            var clock = new ManualClock<long>(1000);
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
            var rawClock = new ManualClock<long>(1000);
            rawClock.Set(333);
            var clock = new Clock<long, ManualClock<long>>(rawClock);
            Assert.AreEqual(0, clock.Count);
            rawClock.Add(100);
            Assert.AreEqual(100, clock.Count);
        }

        [Test]
        public void RtClock_Count()
        {
            const long testSeconds = 123;

            const long rawCountPerSecond = 10_000_000;
            var rawClock = new ManualClock<long>(rawCountPerSecond);

            var rtClock = new RtClock<ManualClock<long>>(rawClock);

            rawClock.Add(testSeconds * rawCountPerSecond);

            var rt = rtClock.Count;
            Assert.AreEqual(testSeconds, rt / RtClock.RtPerSec);

            // var rtPoint = rtClock.ClockPoint();
            // rtPoint.
        }
    }
}