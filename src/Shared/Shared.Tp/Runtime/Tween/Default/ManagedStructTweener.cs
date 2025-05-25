using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using Shared.Sys.Rtt;

namespace Shared.Tp.Tween.Default
{
    public unsafe class ManagedStructTweener<T> : BaseTweener, ITweener<T>
        where T : struct
    {
        public ManagedStructTweener(TweenerProvider provider) : base(provider)
        {
            //TODO: get method once
            var registerField = GetType().GetMethod(nameof(RegisterField), BindingFlags.NonPublic | BindingFlags.Instance);

            var rttType = RttType.Get<T>();
            foreach (var rttField in rttType.PublicFields)
            {
                var genericMethod = registerField!.MakeGenericMethod(rttField.FieldType);
                genericMethod.Invoke(this, new object[] { rttField });
            }
        }

        private void RegisterField<TField>(RttField rttField)
        {
            if (!rttField.HasRuntimeOffset)
            {
                var field = rttField.FieldInfo;
                throw new InvalidOperationException($"Unsupported implicit boxing on field {field.Name} of type {typeof(T).FullName}");
            }

            var tweener = Provider.Get<TField>();
            var offset = rttField.RuntimeOffset;
            var tweenEnabled = rttField.FieldInfo.GetCustomAttribute<TweenAttribute>() != null;
            if (tweenEnabled)
                RegisterProcessor((srcInt0, srcInt1, t, dstInt) =>
                {
                    ref var src0 = ref Unsafe.AsRef<TField>((void*)(srcInt0 + offset));
                    ref var src1 = ref Unsafe.AsRef<TField>((void*)(srcInt1 + offset));
                    ref var dst = ref Unsafe.AsRef<TField>((void*)(dstInt + offset));
                    tweener.Process(ref dst, t, in src0, in src1);
                });
            else
                RegisterProcessor((_, srcInt1, _, dstInt) =>
                {
                    ref var src1 = ref Unsafe.AsRef<TField>((void*)(srcInt1 + offset));
                    ref var dst = ref Unsafe.AsRef<TField>((void*)(dstInt + offset));
                    tweener.Replica(ref dst, in src1);
                });
        }

        public void Replica(ref T dst, in T src)
        {
            dst = src;
            //TODO: replicate all managed fields instead of shallow copy
        }

        public void Process(ref T dst, float t, in T src0, in T src1)
        {
            var srcInt0 = (IntPtr)Unsafe.AsPointer(ref Unsafe.AsRef(src0));
            var srcInt1 = (IntPtr)Unsafe.AsPointer(ref Unsafe.AsRef(src1));
            var dstInt = (IntPtr)Unsafe.AsPointer(ref dst);
            foreach (var processor in Processors)
                processor(srcInt0, srcInt1, t, dstInt);
        }
    }
}