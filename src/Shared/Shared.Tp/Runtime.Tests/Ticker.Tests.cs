using NUnit.Framework;

namespace Shared.Tp.Tests
{
    public class TickerTests
    {
        [Test]
        public void Cycle_Diff()
        {
            unchecked
            {
                const ushort start = (ushort)-10;
                const ushort end = 10;
                const ushort diff = (ushort)(end - start);
                Assert.AreEqual(diff, 20);
            }
        }
    }
}