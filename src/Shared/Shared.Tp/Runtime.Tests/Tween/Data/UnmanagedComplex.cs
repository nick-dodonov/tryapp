using NUnit.Framework;
using Shared.Tp.Tween;

namespace Shared.Tp.Tests.Tween.Data
{
    public struct UnmanagedComplex
    {
        public long Offset; // NO tween
        [Tween] 
        public UnmanagedBasic UnmanagedBasic;

        public static UnmanagedComplex Make(int idx)
        {
            return new()
            {
                Offset = 1111 * (idx + 1),
                UnmanagedBasic = UnmanagedBasic.Make(idx),
            };
        }

        public void AssertInRange(UnmanagedComplex a, UnmanagedComplex b)
        {
            Assert.That(Offset, Is.EqualTo(b.Offset));
            UnmanagedBasic.AssertInRange(a.UnmanagedBasic, b.UnmanagedBasic);
        }
    }
}