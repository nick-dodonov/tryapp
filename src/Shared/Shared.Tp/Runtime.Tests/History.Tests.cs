using NUnit.Framework;
using Shared.Tp.St.Sync;

namespace Shared.Tp.Tests
{
    public class HistoryTests
    {
        [Test]
        public void Begin_Add_And_Clear()
        {
            var hist = new History<int, string>(4);
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
            var hist = new History<int, string>(initCapacity);
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

        [Test]
        public void Iterate_Values_Reverse()
        {
            var hist = new History<int, string>(4);
            var frame = 0;
            hist.AddValueRef(++frame) = frame.ToString();
            hist.AddValueRef(++frame) = frame.ToString();
            hist.AddValueRef(++frame) = frame.ToString();

            // iterate without a cycle
            {
                var expectFrame = frame;
                var iterCount = 0;
                foreach (ref var value in hist.ReverseRefValues)
                {
                    Assert.AreEqual((expectFrame--).ToString(), value);
                    ++iterCount;
                }

                Assert.AreEqual(hist.Count, iterCount);
            }

            // iterate with through cycle
            {
                hist.ClearUntil(frame);
                hist.AddValueRef(++frame) = frame.ToString();
                hist.AddValueRef(++frame) = frame.ToString();

                var expectFrame = frame;
                var iterCount = 0;
                foreach (ref var value in hist.ReverseRefValues)
                {
                    Assert.AreEqual((expectFrame--).ToString(), value);
                    ++iterCount;
                }

                Assert.AreEqual(hist.Count, iterCount);
            }
        }

        [Test]
        public void Resize_On_Cycle()
        {
            const int initCapacity = 4;
            var hist = new History<int, string>(initCapacity);
            var frame = 0;
            hist.AddValueRef(++frame) = frame.ToString();
            hist.AddValueRef(++frame) = frame.ToString();
            hist.AddValueRef(++frame) = frame.ToString();
            hist.ClearUntil(frame);
            hist.AddValueRef(++frame) = frame.ToString();
            hist.AddValueRef(++frame) = frame.ToString();
            hist.AddValueRef(++frame) = frame.ToString();
            Assert.AreEqual(4, hist.Count);
            Assert.AreEqual(initCapacity, hist.Capacity);

            hist.AddValueRef(++frame) = frame.ToString();
            Assert.AreEqual(5, hist.Count);
            Assert.Greater(hist.Capacity, initCapacity);

            var expectFrame = frame;
            var iterCount = 0;
            foreach (ref var item in hist.ReverseRefItems)
            {
                Assert.AreEqual(expectFrame, item.Key);
                Assert.AreEqual((expectFrame--).ToString(), item.Value);
                ++iterCount;
            }

            Assert.AreEqual(hist.Count, iterCount);
        }

        [Test]
        public void Complex_Key()
        {
            var hist = new History<(int, float), string>(4);
            var frame = 0;
            hist.AddValueRef((++frame, frame / 10.0f)) = frame.ToString();
            hist.AddValueRef((++frame, frame / 10.0f)) = frame.ToString();
            Assert.AreEqual(2, hist.Count);

            hist.ClearUntil((2, 0.0f));
            Assert.AreEqual(1, hist.Count);
        }

        private static void Add(History<byte, string> hist, byte key) => hist.AddValueRef(key) = key.ToString();
        private static HistoryExtensions.BoundsVisitor<byte, string> ExpectKeys(byte expectFrom, byte expectTo)
        {
            return (byte _, ref History<byte, string>.Item from, ref History<byte, string>.Item to) =>
            {
                Assert.AreEqual(expectFrom, from.Key);
                Assert.AreEqual(expectTo, to.Key);
            };
        }

        [Test]
        public void Visit_Bounds_With_CycledKey_Base()
        {
            var hist = new History<byte, string>(4);
            Add(hist, 250);
            Add(hist, 10);
            Assert.AreEqual(2, hist.Count);
            Assert.IsTrue(hist.VisitExistingBounds((byte)0, ExpectKeys(250, 10)));
        }

        [Test]
        public void Visit_Bounds_With_CycledKey_Complex()
        {
            var hist = new History<byte, string>(4);
            Add(hist, 220);
            Add(hist, 240);
            Add(hist, 20);
            Add(hist, 40);
            Assert.AreEqual(4, hist.Count);

            Assert.IsTrue(hist.VisitExistingBounds((byte)50, ExpectKeys(40, 40)));
            Assert.IsTrue(hist.VisitExistingBounds((byte)40, ExpectKeys(20, 40)));
            Assert.IsTrue(hist.VisitExistingBounds((byte)30, ExpectKeys(20, 40)));
            Assert.IsTrue(hist.VisitExistingBounds((byte)10, ExpectKeys(240, 20)));
            Assert.IsTrue(hist.VisitExistingBounds((byte)0, ExpectKeys(240, 20)));
            Assert.IsTrue(hist.VisitExistingBounds((byte)250, ExpectKeys(240, 20)));
            Assert.IsTrue(hist.VisitExistingBounds((byte)230, ExpectKeys(220, 240)));
            Assert.IsTrue(hist.VisitExistingBounds((byte)220, ExpectKeys(220, 240)));
            Assert.IsTrue(hist.VisitExistingBounds((byte)210, ExpectKeys(40, 40)));
        }
    }
}