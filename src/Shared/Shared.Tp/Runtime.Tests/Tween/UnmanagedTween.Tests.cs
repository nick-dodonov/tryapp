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

        [Test]
        public void CycleUShortTweener_Edge_Case()
        {
            ushort dst = 0;
            ushort src0 = ushort.MaxValue - 10 + 1;
            ushort src1 = 20;

            var diff = (ushort)(src1 - src0);
            Assert.AreEqual(30, diff);
            var calc = diff * 0.5f;
            var expected = (ushort)(src0 + calc);
            Assert.AreEqual(5, expected);

            var tweener = new CycleUShortTweener();
            tweener.Process(ref dst, 0.5f, src0, src1);
            Assert.AreEqual(expected, dst);
        }
        
    }
}