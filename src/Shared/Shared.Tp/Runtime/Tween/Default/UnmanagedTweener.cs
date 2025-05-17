using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Shared.Tp.Tween.Default
{
    public unsafe class UnmanagedTweener<T> : BaseTweener, ITweener<T>
        where T : unmanaged
    {
        public UnmanagedTweener(TweenerProvider provider) : base(provider)
        {
            var fields = typeof(T).GetFields(BindingFlags.Instance | BindingFlags.Public);
            var registerUnmanagedField = GetType().GetMethod(nameof(RegisterUnmanagedField), BindingFlags.NonPublic | BindingFlags.Instance);

            foreach (var field in fields)
            {
                var genericMethod = registerUnmanagedField!.MakeGenericMethod(field.FieldType);
                genericMethod.Invoke(this, new object[] { field });
            }
        }

        private void RegisterUnmanagedField<TField>(FieldInfo field)
            where TField : unmanaged
        {
            Debug.Assert(!RuntimeHelpers.IsReferenceOrContainsReferences<TField>());

            var tweener = Provider.Get<TField>();
            var fieldOffset = Marshal.OffsetOf<T>(field.Name).ToInt32();
            RegisterProcessor((aPtr, bPtr, t, rPtr) =>
            {
                ref var a = ref *(TField*)(aPtr + fieldOffset);
                ref var b = ref *(TField*)(bPtr + fieldOffset);
                ref var r = ref *(TField*)(rPtr + fieldOffset);
                tweener.Process(ref a, ref b, t, ref r);
            });
        }

        public void Process(ref T a, ref T b, float t, ref T r)
        {
            fixed (T* aPtr = &a, bPtr = &b, rPtr = &r)
            {
                foreach (var processor in Processors)
                {
                    var aIntPtr = (IntPtr)aPtr;
                    var bIntPtr = (IntPtr)bPtr;
                    var rIntPtr = (IntPtr)rPtr;
                    processor(aIntPtr, bIntPtr, t, rIntPtr);
                }
            }
        }
    }
}