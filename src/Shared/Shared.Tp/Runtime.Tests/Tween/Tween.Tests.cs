using NUnit.Framework;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace Shared.Tp.Tests.Tween
{
    public class TweenTests
    {
        //[Test] public void CheckFailWorks() => Assert.Fail();

        [Test]
        public void UnmanagedBasic_Tween_GCFree()
        {
            var a = UnmanagedBasic.Make(0);
            var b = UnmanagedBasic.Make(1);
            var r = new UnmanagedBasic();

            var tweener = new UnmanagedBasicTweener();
            Assert.That(() =>
            {
                tweener.Process(ref a, ref b, 0.5f, ref r);
            }, Is.Not.AllocatingGCMemory());

            UnmanagedBasic.AssertInRange(a, b, r);
        }

        [Test]
        public void UnmanagedComplex_Tween()
        {
            var a = UnmanagedComplex.Make(0);
            var b = UnmanagedComplex.Make(1);
            var r = default(UnmanagedComplex);

            var provider = new TweenerProvider();
            provider.Register(new UnmanagedBasicTweener());

            var tweener = new FieldRefTweener<UnmanagedComplex>(provider);
            tweener.Process(ref a, ref b, 0.5f, ref r);

            UnmanagedBasic.AssertInRange(a.UnmanagedBasic, b.UnmanagedBasic, r.UnmanagedBasic);
        }
    }
}