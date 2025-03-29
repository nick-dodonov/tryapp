using System.Threading.Tasks;
using NUnit.Framework;

namespace Shared.System.Tests
{
    public class Tests
    {
        [Test]
        public async Task StubAsyncTest()
        {
            await Task.Yield();
            Assert.Pass();
        }
    }
}
