using System.Reflection;
using System.Runtime.CompilerServices;
using NUnit.Framework;
using Shared.Tp.Tests.Tween.Data;
using Shared.Tp.Tween;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace Shared.Tp.Tests.Tween
{
    public struct Basic1
    {
        public int IntValue;
        public BasicUnmanaged BasicUnmanaged;
        public string StringValue;
        public long LongValue;
        public byte ByteValue;

        public static Basic1 Make(int idx)
        {
            return new()
            {
                IntValue = 11 * (idx + 1),
                BasicUnmanaged = BasicUnmanaged.Make(idx),
                StringValue = $"std:{idx}",
                LongValue = 111 * (idx + 1),
                ByteValue = (byte)(1 * (idx + 1))
            };
        }
    }

    public class ManagedStructTween_Tests : BaseTweenTests
    {
        [Test]
        public unsafe void Basic1_GetSetValue_GCFree()
        {
            var val = Basic1.Make(0);
            var field = val.GetType().GetField(nameof(Basic1.StringValue), BindingFlags.Instance | BindingFlags.Public)!;
            field.GetValue(val); //warmup accessor

            var ptr = Unsafe.AsPointer(ref val.StringValue);
            ref var strRef = ref Unsafe.AsRef<string>(ptr);
            
            var obj = (object)val;
            Assert.That(() =>
            {
                field.GetValue(obj); //gc-free
                //ref var refVal = ref val;
                //field.GetValue(refVal); //implicit boxing
            }, Is.Not.AllocatingGCMemory());
            
            Assert.IsTrue(true);
        }
        
        [Test]
        public void BasicStruct_Tween_GCFree()
        {
            var (a, b, r) = MakeTestData(BasicStruct.Make);
            
            var provider = new TweenerProvider();
            var tweener = provider.GetOfVar(ref a);

            tweener.Process(ref a, ref b, 0.5f, ref r); // warmup (Mono.JIT->GC.Alloc)
            Assert.That(() =>
            {
                tweener.Process(ref a, ref b, 0.5f, ref r);
            }, Is.Not.AllocatingGCMemory());
            
            r.AssertInRange(a, b);
        }
        
        [Test]
        public void NestedStruct_Tween_GCFree()
        {
            var (a, b, r) = MakeTestData(NestedStruct.Make);
            
            var provider = new TweenerProvider();
            provider.Register(new CustomBasicUnmanagedTweener());
            var tweener = provider.GetOfVar(ref a);

            tweener.Process(ref a, ref b, 0.5f, ref r); // warmup (Mono.JIT->GC.Alloc)
            Assert.That(() =>
            {
                tweener.Process(ref a, ref b, 0.5f, ref r);
            }, Is.Not.AllocatingGCMemory());
            
            r.AssertInRange(a, b);
        }
    }
}