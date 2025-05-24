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
            //TODO: speedup get method once
            var registerUnmanagedField = GetType().GetMethod(nameof(RegisterUnmanagedField), BindingFlags.NonPublic | BindingFlags.Instance);

            var fields = typeof(T).GetFields(BindingFlags.Instance | BindingFlags.Public);
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

            var tweenEnabled = field.GetCustomAttribute<TweenAttribute>() != null;
            var tweener = tweenEnabled 
                ? Provider.Get<TField>()
                : null;
            var fieldOffset = Marshal.OffsetOf<T>(field.Name).ToInt32();
            if (tweener != null)
                RegisterProcessor((aPtr, bPtr, t, rPtr) =>
                {
                    ref var a = ref *(TField*)(aPtr + fieldOffset);
                    ref var b = ref *(TField*)(bPtr + fieldOffset);
                    ref var r = ref *(TField*)(rPtr + fieldOffset);
                    tweener.Process(ref a, ref b, t, ref r);
                });
            else
                RegisterProcessor((_, bPtr, _, rPtr) =>
                {
                    ref var b = ref *(TField*)(bPtr + fieldOffset);
                    ref var r = ref *(TField*)(rPtr + fieldOffset);
                    r = b;
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