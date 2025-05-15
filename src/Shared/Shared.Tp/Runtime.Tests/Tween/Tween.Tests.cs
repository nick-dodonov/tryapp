using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using NUnit.Framework;
using Shared.Tp.Tests.Tween.Data;
using Shared.Tp.Tween;
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

            var tweener = new UnmanagedTweener<UnmanagedComplex>(provider);
            tweener.Process(ref a, ref b, 0.5f, ref r); // warmup (Mono.JIT->GC.Alloc)
            Assert.That(() =>
            {
                tweener.Process(ref a, ref b, 0.5f, ref r);
            }, Is.Not.AllocatingGCMemory());

            r.AssertInRange(a, b);
        }
        
        // [Test]
        // public unsafe void ManagedBasic_Tween_GCFree()
        // {
        //     var (a, b, r) = MakeTestData(ManagedBasic.Make);
        //     
        //     var provider = new TweenerProvider();
        //     ref var x = ref a;
        //     
        //     var tweener = new ManagedTweener<ManagedBasic>(provider);
        //     tweener.Process(ref a, ref b, 0.5f, ref r); // warmup (Mono.JIT->GC.Alloc)
        //     Assert.That(() =>
        //     {
        //         tweener.Process(ref a, ref b, 0.5f, ref r);
        //     }, Is.Not.AllocatingGCMemory());
        //     
        //     r.AssertInRange(a, b);
        // }

        // [Test]
        // public unsafe void ManagedBasic_RefPtr()
        // {
        //     var a = ManagedBasic.Make(0);
        //     var intOffset = Marshal.OffsetOf<ManagedBasic>(nameof(ManagedBasic.IntValue)).ToInt32();            
        //     var stringOffset = Marshal.OffsetOf<ManagedBasic>(nameof(ManagedBasic.StringValue)).ToInt32();
        //     
        //     var aPtr = Unsafe.AsPointer(ref a);
        //     ref var intRef = ref Unsafe.AsRef<int>((void*)((IntPtr)aPtr + intOffset));
        //     ref var stringRef = ref Unsafe.AsRef<int>((void*)((IntPtr)aPtr + stringOffset));
        // }
        
    }
}