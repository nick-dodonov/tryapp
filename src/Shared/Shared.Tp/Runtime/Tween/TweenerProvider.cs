using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using Shared.Log;
using Shared.Tp.Tween.Default;

namespace Shared.Tp.Tween
{
    public class TweenerProvider
    {
        private readonly Dictionary<Type, ITweener> _tweeners = new();
        private readonly MethodInfo _createDefaultUnmanaged;
        private readonly MethodInfo _createDefaultManagedStruct;

        public TweenerProvider()
        {
            var type = GetType();
            const BindingFlags bindingFlags = BindingFlags.NonPublic | BindingFlags.Instance;

            _createDefaultUnmanaged = type.GetMethod(nameof(CreateDefaultUnmanaged), bindingFlags)!;
            Debug.Assert(_createDefaultUnmanaged != null);
            _createDefaultManagedStruct = type.GetMethod(nameof(CreateDefaultManagedStruct), bindingFlags)!;
            Debug.Assert(_createDefaultManagedStruct != null);

            RegisterWellKnown();
        }

        public void Register<T>(ITweener<T> tweener)
        {
            Slog.Info($"{typeof(T).FullName} -> {tweener.GetType().FullName}");
            _tweeners[typeof(T)] = tweener;
        }

        public ITweener<T> GetOfVar<T>(ref T _) => Get<T>(); //to simplify code (without explicit type declaration)

        public ITweener<T> Get<T>()
        {
            var type = typeof(T);
            if (_tweeners.TryGetValue(type, out var baseTweener))
                return (ITweener<T>)baseTweener;

            if (type.IsValueType)
            {
                MethodInfo genericMethod;
                if (!RuntimeHelpers.IsReferenceOrContainsReferences<T>())
                {
                    if (!type.IsPrimitive)
                        genericMethod = _createDefaultUnmanaged.MakeGenericMethod(type);
                    else
                        throw new InvalidOperationException($"Primitive type {type.FullName} is not registered");
                }
                else
                    genericMethod = _createDefaultManagedStruct.MakeGenericMethod(type);

                var tweener = (ITweener<T>)genericMethod.Invoke(this, null);
                Register(tweener);
                return tweener;
            }

            throw new InvalidOperationException($"Type {type.FullName} is not registered");
        }

        private ITweener<T> CreateDefaultUnmanaged<T>() where T : unmanaged => new UnmanagedTweener<T>(this);
        private ITweener<T> CreateDefaultManagedStruct<T>() where T : unmanaged => new ManagedStructTweener<T>(this);

        private void RegisterWellKnown()
        {
            Register(new IntTweener());
            Register(new LongTweener());
            Register(new FloatTweener());
            Register(new StringTweener());
        }
    }

    public class IntTweener : ITweener<int>
    {
        public void Process(ref int a, ref int b, float t, ref int r) => r = (int)(a + (b - a) * t);
    }

    public class LongTweener : ITweener<long>
    {
        public void Process(ref long a, ref long b, float t, ref long r) => r = (long)(a + (b - a) * t);
    }

    public class FloatTweener : ITweener<float>
    {
        public void Process(ref float a, ref float b, float t, ref float r) => r = a + (b - a) * t;
    }

    internal class StringTweener : ITweener<string>
    {
        public void Process(ref string a, ref string b, float t, ref string r) => r = t < 0.5f ? a : b;
    }
}