using NUnit.Framework;
using Shared.Tp.Tween;

namespace Shared.Tp.Tests.Tween.Data
{
    public struct NestedUnmanaged
    {
        public long Offset; // NO tween
        [Tween] 
        public BasicUnmanaged BasicUnmanaged;

        public static NestedUnmanaged Make(int idx)
        {
            return new()
            {
                Offset = 1111 * (idx + 1),
                BasicUnmanaged = BasicUnmanaged.Make(idx),
            };
        }

        public void AssertInRange(NestedUnmanaged a, NestedUnmanaged b)
        {
            Assert.That(Offset, Is.EqualTo(b.Offset));
            BasicUnmanaged.AssertInRange(a.BasicUnmanaged, b.BasicUnmanaged);
        }
    }
}