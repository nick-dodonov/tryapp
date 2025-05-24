using NUnit.Framework;
using Shared.Tp.Tests.Tween.Data;
using Shared.Tp.Tween;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace Shared.Tp.Tests.Tween
{
    public class UnmanagedTween_Tests : BaseTweenTests
    {
        [Test]
        public void BasicUnmanaged_CustomTween_GCFree()
        {
            var (a, b, r) = MakeTestData(BasicUnmanaged.Make);

            var tweener = new CustomUnmanagedBasicTweener();
            Assert.That(() =>
            {
                tweener.Process(ref a, ref b, 0.5f, ref r);
            }, Is.Not.AllocatingGCMemory());

            r.AssertInRange(a, b);
        }

        [Test]
        public void NestedUnmanaged_Tween_GCFree()
        {
            var (a, b, r) = MakeTestData(NestedUnmanaged.Make);

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