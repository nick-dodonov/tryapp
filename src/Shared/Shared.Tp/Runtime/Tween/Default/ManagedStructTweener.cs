using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using Shared.Log;
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
            var tweenEnabled = rttField.FieldInfo.GetCustomAttribute<TweenAttribute>() != null;
            var tweener = tweenEnabled 
                ? Provider.Get<TField>()
                : null;
            
            if (rttField.HasRuntimeOffset)
            {
                var offset = rttField.RuntimeOffset;
                if (tweener != null)
                    RegisterProcessor((aPtr, bPtr, t, rPtr) =>
                    {
                        ref var a = ref Unsafe.AsRef<TField>((void*)(aPtr + offset));
                        ref var b = ref Unsafe.AsRef<TField>((void*)(bPtr + offset));
                        ref var r = ref Unsafe.AsRef<TField>((void*)(rPtr + offset));
                        tweener.Process(ref a, ref b, t, ref r);
                    });
                else
                    RegisterProcessor((_, bPtr, _, rPtr) =>
                    {
                        ref var r = ref Unsafe.AsRef<TField>((void*)(rPtr + offset));
                        ref var b = ref Unsafe.AsRef<TField>((void*)(bPtr + offset));
                        r = b;
                    });
            }
            else
            {
                var field = rttField.FieldInfo;
                Slog.Warn($"implicit boxing on field {field.Name} of type {typeof(T).FullName}");
                if (tweener != null)
                    RegisterProcessor((aPtr, bPtr, t, rPtr) =>
                    {
                        ref var a = ref Unsafe.AsRef<T>((void*)(aPtr));
                        ref var b = ref Unsafe.AsRef<T>((void*)(bPtr));
                        ref var r = ref Unsafe.AsRef<T>((void*)(rPtr));
                        var af = (TField)field.GetValue(a);
                        var bf = (TField)field.GetValue(b);
                        var rf = (TField)field.GetValue(r);
                        var orf = rf;
                        tweener.Process(ref af, ref bf, t, ref rf);
                        if (!ReferenceEquals(af, orf))
                            field.SetValue(r, rf);
                    });
                else
                    RegisterProcessor((_, bPtr, _, rPtr) =>
                    {
                        ref var r = ref Unsafe.AsRef<T>((void*)(rPtr));
                        ref var b = ref Unsafe.AsRef<T>((void*)(bPtr));
                        var bf = (TField)field.GetValue(b);
                        field.SetValue(r, bf);
                    });
            }
        }

        public void Process(ref T a, ref T b, float t, ref T r)
        {
            var aPtr = Unsafe.AsPointer(ref a);
            var bPtr = Unsafe.AsPointer(ref b);
            var rPtr = Unsafe.AsPointer(ref r);

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