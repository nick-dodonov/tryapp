using System;
using System.Collections.Generic;

namespace Shared.Tp.Tween.Default
{
    public class BaseTweener
    {
        protected readonly TweenerProvider Provider;

        protected delegate void FieldProcessor(IntPtr aPtr, IntPtr bPtr, float t, IntPtr rPtr);
        protected readonly List<FieldProcessor> Processors = new();

        protected BaseTweener(TweenerProvider provider)
        {
            Provider = provider;
        }

        protected void RegisterProcessor(FieldProcessor fieldProcessor)
        {
            //TODO: add check for duplicate fields
            // if (!Processors.TryAdd(field, fieldProcessor))
            //     throw new InvalidOperationException($"Field {field.Name} already registered");
            Processors.Add(fieldProcessor);
        }
    }
}