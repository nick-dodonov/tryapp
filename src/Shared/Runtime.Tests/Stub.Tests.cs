using System.Threading.Tasks;
using NUnit.Framework;

namespace Shared.Tests
{
    public class StubTests
    {
        [Test]
        public async Task StubAsyncTest()
        {
            await Task.Yield();
            Assert.Pass();
        }
    }
}
