using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using NUnit.Framework;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;

// ReSharper disable AccessToStaticMemberViaDerivedType

namespace Shared.Tp.Tests.Tween
{
    public struct TestUnmanaged
    {
        public int IntValue;
        public float FloatValue;

        public static TestUnmanaged Make(int idx)
        {
            return new()
            {
                IntValue = 11 * (idx + 1),
                FloatValue = 11.1f * (idx + 1),
            };
        }

        public static void CustomTween(ref TestUnmanaged a, ref TestUnmanaged b, float t, ref TestUnmanaged r)
        {
            r.IntValue = (int)(a.IntValue * (1 - t) + b.IntValue * t);
            r.FloatValue = a.FloatValue * (1 - t) + b.FloatValue * t;
        }

        public static void AssertInside(in TestUnmanaged a, in TestUnmanaged b, in TestUnmanaged r)
        {
            Assert.That(r.IntValue, Is.InRange(a.IntValue, b.IntValue));
            Assert.That(r.FloatValue, Is.InRange(a.FloatValue, b.FloatValue));
        }
    }

    public struct TestAuto
    {
        public long Offset;
        public TestUnmanaged Unmanaged;
        //public string StringValue;

        public static TestAuto Make(int idx)
        {
            return new()
            {
                Unmanaged = TestUnmanaged.Make(idx),
                //StringValue = $"idx:{idx}"
            };
        }
    }

    public interface ITweenProcessor<TField> where TField : unmanaged
    {
        void Process(ref TField a, ref TField b, float t, ref TField r);
    }

    public class TestUnmanagedProcessor : ITweenProcessor<TestUnmanaged>
    {
        public void Process(ref TestUnmanaged a, ref TestUnmanaged b, float t, ref TestUnmanaged r)
        {
            TestUnmanaged.CustomTween(ref a, ref b, t, ref r);
        }
    }

    public static unsafe class FieldRefTweenProcessor<T> where T : unmanaged
    {
        // ReSharper disable once StaticMemberInGenericType
        private static readonly Dictionary<FieldInfo, Action<IntPtr, IntPtr, float, IntPtr>> _processors = new();

        public static void RegisterProcessor<TField>(FieldInfo field, ITweenProcessor<TField> processor)
            where TField : unmanaged
        {
            var fieldOffset = Marshal.OffsetOf<T>(field.Name).ToInt32();
            _processors[field] = (aPtr, bPtr, t, rPtr) =>
            {
                ref var a = ref *(TField*)(aPtr + fieldOffset);
                ref var b = ref *(TField*)(bPtr + fieldOffset);
                ref var r = ref *(TField*)(rPtr + fieldOffset);
                processor.Process(ref a, ref b, t, ref r);
            };
        }

        public static void Process(ref T a, ref T b, float t, ref T r)
        {
            fixed (T* aPtr = &a, bPtr = &b, rPtr = &r)
            {
                foreach (var (_, processor) in _processors)
                {
                    processor((IntPtr)aPtr, (IntPtr)bPtr, t, (IntPtr)rPtr);
                }
            }
        }
    }

    public class TweenTests
    {
        [Test]
        public void Unmanaged_CustomTween_GCFree()
        {
            var a = TestUnmanaged.Make(0);
            var b = TestUnmanaged.Make(1);
            var r = new TestUnmanaged();

            Assert.That(() =>
            {
                TestUnmanaged.CustomTween(ref a, ref b, 0.5f, ref r);
            }, Is.Not.AllocatingGCMemory());

            TestUnmanaged.AssertInside(a, b, r);
        }

        [Test]
        public void FieldRefTweenProcessor_ImplChecks()
        {
            var a = TestAuto.Make(0);
            var b = TestAuto.Make(1);
            var r = default(TestAuto);

            {
                var fields = typeof(TestAuto).GetFields(BindingFlags.Instance | BindingFlags.Public);
                foreach (var field in fields)
                {
                    if (field.FieldType == typeof(TestUnmanaged))
                    {
                        FieldRefTweenProcessor<TestAuto>.RegisterProcessor(field, new TestUnmanagedProcessor());
                    }
                }
                
                FieldRefTweenProcessor<TestAuto>.Process(ref a, ref b, 0.5f, ref r);
                TestUnmanaged.AssertInside(a.Unmanaged, b.Unmanaged, r.Unmanaged);
            }
            //TODO: impl checks
        }
    }
}