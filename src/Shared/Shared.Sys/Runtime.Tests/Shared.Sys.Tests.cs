using System.Threading.Tasks;
using NUnit.Framework;

namespace Shared.Sys.Tests
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
