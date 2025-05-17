using NUnit.Framework;
using Shared.Tp.Tests.Tween.Data;
using Shared.Tp.Tween;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace Shared.Tp.Tests.Tween
{
    public class UnmanagedTweenTests : BaseTweenTests
    {
        [Test]
        public void UnmanagedBasic_CustomTween_GCFree()
        {
            var (a, b, r) = MakeTestData(UnmanagedBasic.Make);

            var tweener = new CustomUnmanagedBasicTweener();
            Assert.That(() =>
            {
                tweener.Process(ref a, ref b, 0.5f, ref r);
            }, Is.Not.AllocatingGCMemory());

            r.AssertInRange(a, b);
        }

        [Test]
        public void UnmanagedComplex_Tween_GCFree()
        {
            var (a, b, r) = MakeTestData(UnmanagedComplex.Make);

            var provider = new TweenerProvider();
            provider.Register(new CustomUnmanagedBasicTweener());
            var tweener= provider.GetOfVar(ref a);

            tweener.Process(ref a, ref b, 0.5f, ref r); // warmup (Mono.JIT->GC.Alloc)
            Assert.That(() =>
            {
                tweener.Process(ref a, ref b, 0.5f, ref r);
            }, Is.Not.AllocatingGCMemory());

            r.AssertInRange(a, b);
        }
    }
}