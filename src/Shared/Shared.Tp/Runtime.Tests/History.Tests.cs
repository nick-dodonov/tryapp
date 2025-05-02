using NUnit.Framework;
using Shared.Tp.St.Sync;

namespace Shared.Tp.Tests
{
    public class HistoryTests
    {
        [Test]
        public void Add_And_Clear()
        {
            var hist = new History<string>();
            Assert.AreEqual(0, hist.Count);

            hist.AddValue(1, "1");
            hist.AddValue(2, "2");
            Assert.AreEqual(2, hist.Count);
            
            hist.ClearUntil(1);
            Assert.AreEqual(2, hist.Count);

            hist.ClearUntil(2);
            Assert.AreEqual(1, hist.Count);
        }
    }
}