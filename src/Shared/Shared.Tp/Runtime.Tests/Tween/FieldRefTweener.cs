using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Shared.Tp.Tests.Tween
{
    public class FieldRefTweener
    {
        protected readonly TweenerProvider Provider;

        protected delegate void FieldProcessor(IntPtr aPtr, IntPtr bPtr, float t, IntPtr rPtr);
        protected readonly Dictionary<FieldInfo, FieldProcessor> Processors = new(); //TODO: don't need map, just use array to speedup processing

        protected FieldRefTweener(TweenerProvider provider)
        {
            Provider = provider;
        }

        protected void RegisterProcessor(FieldInfo field, FieldProcessor fieldProcessor)
        {
            if (!Processors.TryAdd(field, fieldProcessor))
                throw new InvalidOperationException($"Field {field.Name} already registered");
        }
    }

    public unsafe class FieldRefTweener<T> : FieldRefTweener, ITweener<T>
        where T : unmanaged
    {
        public FieldRefTweener(TweenerProvider provider) : base(provider)
        {
            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
                throw new NotImplementedException("TODO: support reference types");

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
            if (RuntimeHelpers.IsReferenceOrContainsReferences<TField>())
                throw new NotImplementedException("TODO: support reference field types");

            var tweener = Provider.Get<TField>();
            var fieldOffset = Marshal.OffsetOf<T>(field.Name).ToInt32();
            RegisterProcessor(field, (aPtr, bPtr, t, rPtr) =>
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
                foreach (var (_, processor) in Processors)
                    processor((IntPtr)aPtr, (IntPtr)bPtr, t, (IntPtr)rPtr);
            }
        }
    }
}