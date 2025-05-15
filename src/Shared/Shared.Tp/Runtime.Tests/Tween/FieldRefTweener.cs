using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Shared.Tp.Tests.Tween
{
    public unsafe class FieldRefTweener<T>
        : ITweener<T>
        where T : unmanaged
    {
        private readonly TweenerProvider _provider;

        private delegate void FieldProcessor(IntPtr aPtr, IntPtr bPtr, float t, IntPtr rPtr);
        private readonly Dictionary<FieldInfo, FieldProcessor> _processors = new();

        public FieldRefTweener(TweenerProvider provider)
        {
            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
                throw new NotImplementedException("TODO: support reference types");
            
            _provider = provider;
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

            var tweener = _provider.Get<TField>();
            RegisterProcessor(field, tweener);
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
}