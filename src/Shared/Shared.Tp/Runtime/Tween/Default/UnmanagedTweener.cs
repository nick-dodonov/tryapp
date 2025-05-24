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
                RegisterProcessor((srcInt0, srcInt1, t, dstInt) =>
                {
                    ref var src0 = ref *(TField*)(srcInt0 + fieldOffset);
                    ref var src1 = ref *(TField*)(srcInt1 + fieldOffset);
                    ref var dst = ref *(TField*)(dstInt + fieldOffset);
                    tweener.Process(in src0, in src1, t, ref dst);
                });
            else
                RegisterProcessor((_, srcInt1, _, dstInt) =>
                {
                    ref var src1 = ref *(TField*)(srcInt1 + fieldOffset);
                    ref var dst = ref *(TField*)(dstInt + fieldOffset);
                    dst = src1;
                });
        }

        public void Process(in T src0, in T src1, float t, ref T dst)
        {
            fixed (T* srcPtr0 = &src0, srcPtr1 = &src1, dstPtr = &dst)
            {
                var srcInt0 = (IntPtr)srcPtr0;
                var srcInt1 = (IntPtr)srcPtr1;
                var dstInt = (IntPtr)dstPtr;
                foreach (var processor in Processors)
                    processor(srcInt0, srcInt1, t, dstInt);
            }
        }
    }
}