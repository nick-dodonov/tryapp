using System;
using System.Collections.Generic;

namespace Shared.Tp.Tests.Tween
{
    public class TweenerProvider
    {
        private readonly Dictionary<Type, ITweener> _tweeners = new();

        public TweenerProvider()
        {
            RegisterWellKnown();
        }

        public void Register<T>(ITweener<T> tweener) => _tweeners[typeof(T)] = tweener;
        public ITweener Get(Type type) => _tweeners[type];

        private void RegisterWellKnown()
        {
            Register(new LongTweener());
        }
    }

    public class LongTweener : ITweener<long>
    {
        void ITweener<long>.Process(ref long a, ref long b, float t, ref long r)
        {
            r = (long)(a + (b - a) * t);
        }
    }
}