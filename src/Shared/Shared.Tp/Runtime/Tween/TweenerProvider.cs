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
        public void Replica(in int src, ref int dst) => dst = src;
        public void Process(in int src0, in int src1, float t, ref int dst) => dst = (int)(src0 + (src1 - src0) * t);
    }

    public class LongTweener : ITweener<long>
    {
        public void Replica(in long src, ref long dst) => dst = src;
        public void Process(in long src0, in long src1, float t, ref long dst) => dst = (long)(src0 + (src1 - src0) * t);
    }

    public class FloatTweener : ITweener<float>
    {
        public void Replica(in float src, ref float dst) => dst = src;
        public void Process(in float src0, in float src1, float t, ref float dst) => dst = src0 + (src1 - src0) * t;
    }

    internal class StringTweener : ITweener<string>
    {
        public void Replica(in string src, ref string dst) => dst = src;
        public void Process(in string src0, in string src1, float t, ref string dst) => dst = t < 0.5f ? src0 : src1;
    }
}