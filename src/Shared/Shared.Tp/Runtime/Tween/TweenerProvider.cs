using System;
using System.Collections.Generic;

namespace Shared.Tp.Tween
{
    public class TweenerProvider
    {
        private readonly Dictionary<Type, ITweener> _tweeners = new();

        public TweenerProvider()
        {
            RegisterWellKnown();
        }

        public void Register<T>(ITweener<T> tweener) => _tweeners[typeof(T)] = tweener;
        public ITweener<T> Get<T>() => (ITweener<T>)_tweeners[typeof(T)];

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