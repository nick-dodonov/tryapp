using NUnit.Framework;
using Shared.Tp.St.Sync;

namespace Shared.Tp.Tests
{
    public class StHistoryTests
    {
        [Test]
        public void Visit_Bounds()
        {
            var hist = new StHistory<string>(4);
            {
                var visited = 0;
                hist.VisitExistingBounds((0, 5000),
                    (StKey _, ref StHistory<string>.Item _, ref StHistory<string>.Item _) => ++visited);
                Assert.AreEqual(0, visited);
            }

            var frame = 0;
            // ReSharper disable once UselessBinaryOperation
            hist.AddValueRef((++frame, frame * 1000)) = frame.ToString();
            hist.AddValueRef((++frame, frame * 1000)) = frame.ToString();
            hist.AddValueRef((++frame, frame * 1000)) = frame.ToString();
            Assert.AreEqual(3, hist.Count);

            // [1 2 3] X
            {
                var visited = 0;
                hist.VisitExistingBounds((0, 5000),
                    (StKey key, ref StHistory<string>.Item from, ref StHistory<string>.Item to) =>
                    {
                        ++visited;
                        Assert.AreEqual(0, key.Frame);
                        Assert.AreEqual(5000, key.Ms);
                        Assert.AreEqual(3, from.Key.Frame);
                        Assert.AreEqual(3, to.Key.Frame);
                    });
                Assert.AreEqual(1, visited);
            }
            
            // [1 2 X 3]
            {
                var visited = 0;
                hist.VisitExistingBounds((0, 2500),
                    (StKey _, ref StHistory<string>.Item from, ref StHistory<string>.Item to) =>
                    {
                        ++visited;
                        Assert.AreEqual(2, from.Key.Frame);
                        Assert.AreEqual(3, to.Key.Frame);
                    });
                Assert.AreEqual(1, visited);
            }
            
            // [1 X 2 3]
            {
                var visited = 0;
                hist.VisitExistingBounds((0, 1500),
                    (StKey _, ref StHistory<string>.Item from, ref StHistory<string>.Item to) =>
                    {
                        ++visited;
                        Assert.AreEqual(1, from.Key.Frame);
                        Assert.AreEqual(2, to.Key.Frame);
                    });
                Assert.AreEqual(1, visited);
            }
            
            // X [1 2 3]
            {
                var visited = 0;
                hist.VisitExistingBounds((0, 500),
                    (StKey _, ref StHistory<string>.Item from, ref StHistory<string>.Item to) =>
                    {
                        ++visited;
                        Assert.AreEqual(1, from.Key.Frame);
                        Assert.AreEqual(1, to.Key.Frame);
                    });
                Assert.AreEqual(1, visited);
            }
            
            // [1 2 X=3]
            {
                var visited = 0;
                hist.VisitExistingBounds((0, 3000),
                    (StKey _, ref StHistory<string>.Item from, ref StHistory<string>.Item to) =>
                    {
                        ++visited;
                        Assert.AreEqual(2, from.Key.Frame);
                        Assert.AreEqual(3, to.Key.Frame);
                    });
                Assert.AreEqual(1, visited);
            }

            // [1 2=X 3]
            {
                var visited = 0;
                hist.VisitExistingBounds((0, 2000),
                    (StKey _, ref StHistory<string>.Item from, ref StHistory<string>.Item to) =>
                    {
                        ++visited;
                        Assert.AreEqual(2, from.Key.Frame);
                        Assert.AreEqual(3, to.Key.Frame);
                    });
                Assert.AreEqual(1, visited);
            }
            
            // [1=X 2 3]
            {
                var visited = 0;
                hist.VisitExistingBounds((0, 1000),
                    (StKey _, ref StHistory<string>.Item from, ref StHistory<string>.Item to) =>
                    {
                        ++visited;
                        Assert.AreEqual(1, from.Key.Frame);
                        Assert.AreEqual(2, to.Key.Frame);
                    });
                Assert.AreEqual(1, visited);
            }
        }

        private static int _frame;
        private static void Add(StHistory<string> hist, int key) => hist.AddValueRef((++_frame, key)) = key.ToString();
        
        [Test]
        public void Visit_Bounds_Edge_Case()
        {
            var hist = new StHistory<string>(4);
            //Ⓢ ServerStateView: Update: FR=11531: 64805: [1268 1268]: 63537/0=0 - [1268, 64794, 62784, 60774]
            Add(hist, 60774);
            Add(hist, 62784);
            Add(hist, 64794);
            Add(hist, 1268);
            Assert.IsTrue(hist.VisitCycleKeyBounds((0, 64805),(StKey _, ref StHistory<string>.Item from, ref StHistory<string>.Item to) =>
            {
                var fromKey = from.Key.Ms;
                var toKey = to.Key.Ms;
                Assert.AreEqual(64794, fromKey);
                Assert.AreEqual(1268, toKey);
            }));
        }


    }
}