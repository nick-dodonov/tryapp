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
                Assert.AreEqual(expectFrame, item.key);
                Assert.AreEqual((expectFrame--).ToString(), item.value);
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
    }
}