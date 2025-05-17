using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using Shared.Log;

namespace Shared.Tp.Tween
{
    public class TweenerProvider
    {
        private readonly Dictionary<Type, ITweener> _tweeners = new();
        private readonly MethodInfo _createDefaultUnmanaged;

        public TweenerProvider()
        {
            _createDefaultUnmanaged = GetType().GetMethod(nameof(CreateDefaultUnmanaged), BindingFlags.NonPublic | BindingFlags.Instance)!;
            Debug.Assert(_createDefaultUnmanaged != null);
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
            if (_tweeners.TryGetValue(typeof(T), out var baseTweener))
                return (ITweener<T>)baseTweener;

            if (!RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            {
                var genericMethod = _createDefaultUnmanaged.MakeGenericMethod(typeof(T));
                var tweener = (ITweener<T>)genericMethod.Invoke(this, null);
                Register(tweener);
                return tweener;
            }
            
            throw new InvalidOperationException($"Type {typeof(T).FullName} is not registered");
        }

        private ITweener<T> CreateDefaultUnmanaged<T>() where T : unmanaged
        {
            return new UnmanagedTweener<T>(this);
        }

        private void RegisterWellKnown()
        {
            Register(new IntTweener());
            Register(new LongTweener());
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

    internal class StringTweener : ITweener<string>
    {
        public void Process(ref string a, ref string b, float t, ref string r) => r = t < 0.5f ? a : b;
    }
}