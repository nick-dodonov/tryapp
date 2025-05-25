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

            var tweener = new CustomBasicUnmanagedTweener();
            Assert.That(() =>
            {
                tweener.Process(ref r, 0.5f, in a, in b);
            }, Is.Not.AllocatingGCMemory());

            r.AssertInRange(a, b);
        }

        [Test]
        public void NestedUnmanaged_Tween_GCFree()
        {
            var (a, b, r) = MakeTestData(NestedUnmanaged.Make);

            var provider = new TweenerProvider();
            provider.Register(new CustomBasicUnmanagedTweener());
            var tweener= provider.GetOfVar(ref a);

            tweener.Process(ref r, 0.5f, in a, in b); // warmup (Mono.JIT->GC.Alloc)
            Assert.That(() =>
            {
                tweener.Process(ref r, 0.5f, in a, in b);
            }, Is.Not.AllocatingGCMemory());

            r.AssertInRange(a, b);
        }
    }
}