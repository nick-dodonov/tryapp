using System.Threading.Tasks;
using NUnit.Framework;
using Shared.Tp.Chrono;

namespace Shared.Tp.Tests.Chrono
{
    using TestClock = UserClock<long, TestPeriod>;
    using UnityClock = UserClock<float, SecPeriod>;

    public class Clock_Tests
    {
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
            var testClock = new TestClock(1000);
            testClock.Set(333);
            var deltaClock = new DeltaClock<long, TestPeriod, TestClock>(testClock);
            Assert.AreEqual(0, deltaClock.Count);
            testClock.Add(100);
            Assert.AreEqual(100, deltaClock.Count);
        }

        [Test]
        public void UnityClock_Count()
        {
            var unityClock = new UnityClock();
            unityClock.Set(11.22f);
            var deltaClock = new DeltaClock<float, SecPeriod, UnityClock>(unityClock);
            Assert.AreEqual(0.0f, deltaClock.Count);
            unityClock.Add(33.44f);
            Assert.AreEqual(33.44f, deltaClock.Count);
        }
    }
}