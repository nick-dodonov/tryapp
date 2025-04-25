using System;
using NUnit.Framework;
using Shared.Tp.Util;

namespace Shared.Tests.Tp
{
    public class PrependBufferWriterTests
    {
        [Test]
        public void OnlyReserved_And_Resize()
        {
            var reservedBytes = new byte[] {1, 2, 3};
            
            using var host = PooledBufferWriter.Rent();
            Assert.Zero(host.WrittenCount);
            var initCapacity = host.Capacity;

            {
                using var overlay = PrependBufferWriter.Rent(host, reservedBytes.Length);
                reservedBytes.AsSpan().CopyTo(overlay.ReservedSpan);
                Assert.Zero(host.WrittenCount);

                overlay.GetMemory(initCapacity + 1);
                Assert.Greater(host.Capacity, initCapacity);
                
                Assert.Zero(host.WrittenCount);
            }

            Assert.AreEqual(reservedBytes.Length, host.WrittenCount);
            Assert.That(host.WrittenSpan.SequenceEqual(reservedBytes));
        }

        [Test]
        public void OnlyAdvanced_And_Resize()
        {
            var advancedBytes = new byte[] {1, 2, 3, 4, 5, 6, 7};
            
            using var host = PooledBufferWriter.Rent();
            Assert.Zero(host.WrittenCount);

            {
                using var overlay = PrependBufferWriter.Rent(host, 0);
                Assert.Zero(host.WrittenCount);
                advancedBytes.AsSpan().CopyTo(overlay.GetSpan(advancedBytes.Length));
                overlay.Advance(advancedBytes.Length);
                Assert.Zero(host.WrittenCount);

                var capacity = host.Capacity;
                overlay.GetMemory(capacity + 1);
                Assert.Greater(host.Capacity, capacity);
                
                Assert.Zero(host.WrittenCount);
            }

            Assert.AreEqual(advancedBytes.Length, host.WrittenCount);
            Assert.That(host.WrittenSpan.SequenceEqual(advancedBytes));
        }
    }
}