using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using NUnit.Framework;
using Shared.Tp.Tests.Tween.Data;
using Shared.Tp.Tween;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace Shared.Tp.Tests.Tween
{
    public unsafe struct TypedRef
    {
        public void* Ptr;
        public string Name;
        public IntPtr Offset;

        public TypedRef(void* ptr, string name, IntPtr offset)
        {
            Ptr = ptr;
            Name = name;
            Offset = offset;
        }
    }
    
    //[StructLayout(LayoutKind.Sequential)]
    public struct Basic1
    {
        public int IntValue;
        public UnmanagedBasic UnmanagedBasic;
        public string StringValue;
        public long LongValue;
        public byte ByteValue;

        public static Basic1 Make(int idx)
        {
            return new()
            {
                IntValue = 11 * (idx + 1),
                UnmanagedBasic = UnmanagedBasic.Make(idx),
                StringValue = $"std:{idx}",
                LongValue = 111 * (idx + 1),
                ByteValue = (byte)(1 * (idx + 1))
            };
        }

        public unsafe TypedRef[] GetFieldPointers()
        {
            return new TypedRef[]
            {
                new(Unsafe.AsPointer(ref IntValue), nameof(IntValue), Marshal.OffsetOf<Basic1>(nameof(IntValue))),
                new(Unsafe.AsPointer(ref UnmanagedBasic), nameof(UnmanagedBasic), Marshal.OffsetOf<Basic1>(nameof(UnmanagedBasic))),
                new(Unsafe.AsPointer(ref StringValue), nameof(StringValue), Marshal.OffsetOf<Basic1>(nameof(StringValue))),
                new(Unsafe.AsPointer(ref LongValue), nameof(LongValue), Marshal.OffsetOf<Basic1>(nameof(LongValue))),
                new(Unsafe.AsPointer(ref ByteValue), nameof(ByteValue), Marshal.OffsetOf<Basic1>(nameof(ByteValue))),
            };
        }
    }

    public class ManagedTweenTests : BaseTweenTests
    {
        [Test]
        public unsafe void ManagedBasic_RefPtr()
        {
            var type = typeof(Basic1);
            //var uno = RuntimeHelpers.GetUninitializedObject(type);
            
            var obj = Basic1.Make(0);
            
            var fp = obj.GetFieldPointers();
            var offsets = fp.Select(x => ((int)((byte*)x.Ptr - (byte*)Unsafe.AsPointer(ref obj)), x.Name, x.Offset)).ToArray();

            RuntimeHelpers.GetObjectValue(obj);
            
            ref var refObj = ref obj;
            Handle(ref refObj, ref refObj.IntValue, type.GetField(nameof(Basic1.IntValue), BindingFlags.Instance | BindingFlags.Public)!);
            Handle(ref refObj, ref refObj.UnmanagedBasic, type.GetField(nameof(Basic1.UnmanagedBasic), BindingFlags.Instance | BindingFlags.Public)!);
            Handle(ref refObj, ref refObj.LongValue, type.GetField(nameof(Basic1.LongValue), BindingFlags.Instance | BindingFlags.Public)!);
            Handle(ref refObj, ref refObj.ByteValue, type.GetField(nameof(Basic1.ByteValue), BindingFlags.Instance | BindingFlags.Public)!);
            
            Assert.IsTrue(true);
        }

        private static unsafe void Handle<T, TField>(ref T obj, ref TField field, FieldInfo fieldInfo)
        {
            var objPtr = (byte*)Unsafe.AsPointer(ref obj);
            var fieldPtr = (byte*)Unsafe.AsPointer(ref field);
            var fieldOffset = Marshal.OffsetOf<T>(fieldInfo.Name).ToInt32();
            var fieldRealOffset = (int)(fieldPtr - objPtr);
            
            var objFieldsOffset = (int)(fieldRealOffset - fieldOffset);
            ref var fieldRef = ref Unsafe.AsRef<TField>((void*)((IntPtr)objPtr + fieldRealOffset));
            
            Assert.IsTrue(true);
        }

        [Test]
        public void ManagedBasic_Tween_GCFree()
        {
            var (a, b, r) = MakeTestData(ManagedBasic.Make);
            
            var provider = new TweenerProvider();
            var tweener = provider.GetOfVar(ref a);

            Assert.That(() =>
            {
                tweener.Process(ref a, ref b, 0.5f, ref r);
            }, Is.Not.AllocatingGCMemory());
            
            r.AssertInRange(a, b);
        }
    }
}