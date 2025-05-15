using System;
using System.Collections.Generic;
using System.Reflection;

namespace Shared.Tp.Tween
{
    public class BaseTweener
    {
        protected readonly TweenerProvider Provider;

        protected delegate void FieldProcessor(IntPtr aPtr, IntPtr bPtr, float t, IntPtr rPtr);
        protected readonly Dictionary<FieldInfo, FieldProcessor> Processors = new(); //TODO: don't need map, just use array to speedup processing

        protected BaseTweener(TweenerProvider provider)
        {
            Provider = provider;
        }

        protected void RegisterProcessor(FieldInfo field, FieldProcessor fieldProcessor)
        {
            if (!Processors.TryAdd(field, fieldProcessor))
                throw new InvalidOperationException($"Field {field.Name} already registered");
        }
    }
}