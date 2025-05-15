using NUnit.Framework;

namespace Shared.Tp.Tests.Tween.Data
{
    public struct UnmanagedComplex
    {
        public long Offset;
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
            Assert.That(Offset, Is.InRange(a.Offset, b.Offset));
            UnmanagedBasic.AssertInRange(a.UnmanagedBasic, b.UnmanagedBasic);
        }
    }
}