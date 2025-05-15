using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Shared.Tp.Tween
{
    public unsafe class ManagedTweener<T> : BaseTweener, ITweener<T>
        where T : struct
    {
        public ManagedTweener(TweenerProvider provider) : base(provider)
        {
            var fields = typeof(T).GetFields(BindingFlags.Instance | BindingFlags.Public);
            var registerField = GetType().GetMethod(nameof(RegisterField), BindingFlags.NonPublic | BindingFlags.Instance);

            foreach (var field in fields)
            {
                // if (!field.FieldType.IsValueType)
                //     throw new InvalidOperationException($"Field {field.Name} type must be a value type");

                var genericMethod = registerField!.MakeGenericMethod(field.FieldType);
                genericMethod.Invoke(this, new object[] { field });
            }
        }

        private void RegisterField<TField>(FieldInfo field)
        {
            var tweener = Provider.Get<TField>();
            //if (!RuntimeHelpers.IsReferenceOrContainsReferences<TField>())
            if (field.FieldType.IsValueType)
            {
                //WRONG
                // var fieldOffset = Marshal.OffsetOf<T>(field.Name).ToInt32();
                // RegisterProcessor(field, (aPtr, bPtr, t, rPtr) =>
                // {
                //     ref var a = ref Unsafe.AsRef<TField>((void*)(aPtr + fieldOffset));
                //     ref var b = ref Unsafe.AsRef<TField>((void*)(bPtr + fieldOffset));
                //     ref var r = ref Unsafe.AsRef<TField>((void*)(rPtr + fieldOffset));
                //     tweener.Process(ref a, ref b, t, ref r);
                // });
            }
            else
            {
                RegisterProcessor(field, (aPtr, bPtr, t, rPtr) =>
                {
                    ref var a = ref Unsafe.AsRef<T>((void*)(aPtr));
                    ref var b = ref Unsafe.AsRef<T>((void*)(bPtr));
                    ref var r = ref Unsafe.AsRef<T>((void*)(rPtr));
                    var af = (TField?)field.GetValue(a);
                    var bf = (TField?)field.GetValue(b);
                    var rf = (TField?)field.GetValue(r);
                    var orf = rf;
                    tweener.Process(ref af!, ref bf!, t, ref rf!);
                    if (!ReferenceEquals(af, orf))
                        field.SetValue(r, rf);
                });
            }
        }

        public void Process(ref T a, ref T b, float t, ref T r)
        {
            var aPtr = Unsafe.AsPointer(ref a);
            var bPtr = Unsafe.AsPointer(ref b);
            var rPtr = Unsafe.AsPointer(ref r);

            foreach (var (_, processor) in Processors)
            {
                var aIntPtr = (IntPtr)aPtr;
                var bIntPtr = (IntPtr)bPtr;
                var rIntPtr = (IntPtr)rPtr;
                processor(aIntPtr, bIntPtr, t, rIntPtr);
            }
        }
    }
}