using System;
using NUnit.Framework;
using Shared.Tp.Tests.Tween.Data;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace Shared.Tp.Tests.Tween
{
    public class TweenTests
    {
        //[Test] public void CheckFailWorks() => Assert.Fail();
        private static (T, T, T) MakeTestData<T>(Func<int, T> factory) where T : new() => (factory(0), factory(1), new());

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

            var tweener = new FieldRefTweener<UnmanagedComplex>(provider);
            tweener.Process(ref a, ref b, 0.5f, ref r); // warmup (Mono.JIT->GC.Alloc)
            Assert.That(() =>
            {
                tweener.Process(ref a, ref b, 0.5f, ref r);
            }, Is.Not.AllocatingGCMemory());

            r.AssertInRange(a, b);
        }
    }
}