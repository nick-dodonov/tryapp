using NUnit.Framework;
using Shared.Tp.St.Sync;

namespace Shared.Tp.Tests
{
    public class HistoryTests
    {
        [Test]
        public void Start_Add_And_Clear()
        {
            var hist = new History<string>(4);
            Assert.AreEqual(0, hist.Count);

            hist.AddValueRef(1) = "1";
            hist.AddValueRef(2) = "2";
            Assert.AreEqual(2, hist.Count);
            
            hist.ClearUntil(1);
            Assert.AreEqual(2, hist.Count);

            hist.ClearUntil(2);
            Assert.AreEqual(1, hist.Count);
        }

        [Test]
        public void Cycle_Add_And_Clear()
        {
            const int initCapacity = 4;
            var hist = new History<string>(initCapacity);
            Assert.AreEqual(initCapacity, hist.Capacity);

            var frame = 0;
            hist.AddValueRef(++frame) = frame.ToString();
            Assert.AreEqual(1, hist.Count);

            for (var i = 0; i < 10; ++i)
            {
                hist.AddValueRef(++frame) = frame.ToString();
                Assert.AreEqual(2, hist.Count);

                hist.ClearUntil(frame);
                Assert.AreEqual(1, hist.Count);
                Assert.AreEqual(frame.ToString(), hist.LastValueRef);
            }
            
            Assert.AreEqual(initCapacity, hist.Capacity);
        }
    }
}