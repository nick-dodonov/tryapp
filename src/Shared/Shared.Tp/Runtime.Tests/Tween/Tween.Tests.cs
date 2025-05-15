using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using NUnit.Framework;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace Shared.Tp.Tests.Tween
{
    public unsafe class FieldRefTweener<T>
        : ITweener<T>
        where T : unmanaged
    {
        private delegate void FieldProcessor(IntPtr aPtr, IntPtr bPtr, float t, IntPtr rPtr);
        private readonly Dictionary<FieldInfo, FieldProcessor> _processors = new();

        public FieldRefTweener()
        {
            var fields = typeof(T).GetFields(BindingFlags.Instance | BindingFlags.Public);
            foreach (var field in fields)
            {
                if (field.FieldType == typeof(UnmanagedBasic))
                    RegisterProcessor(field, new UnmanagedBasicTweener());
            }
        }

        private void RegisterProcessor<TField>(FieldInfo field, ITweener<TField> tweener)
            where TField : unmanaged
        {
            var fieldOffset = Marshal.OffsetOf<T>(field.Name).ToInt32();
            _processors[field] = (aPtr, bPtr, t, rPtr) =>
            {
                ref var a = ref *(TField*)(aPtr + fieldOffset);
                ref var b = ref *(TField*)(bPtr + fieldOffset);
                ref var r = ref *(TField*)(rPtr + fieldOffset);
                tweener.Process(ref a, ref b, t, ref r);
            };
        }

        public void Process(ref T a, ref T b, float t, ref T r)
        {
            fixed (T* aPtr = &a, bPtr = &b, rPtr = &r)
            {
                foreach (var (_, processor) in _processors)
                    processor((IntPtr)aPtr, (IntPtr)bPtr, t, (IntPtr)rPtr);
            }
        }
    }

    public class TweenTests
    {
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

            // var provider = new TweenerProvider();
            // provider.Register(new UnmanagedBasicTweener());

            var tweener = new FieldRefTweener<UnmanagedComplex>();
            tweener.Process(ref a, ref b, 0.5f, ref r);

            UnmanagedBasic.AssertInRange(a.UnmanagedBasic, b.UnmanagedBasic, r.UnmanagedBasic);
        }
    }
}