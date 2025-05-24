using NUnit.Framework;
using Shared.Tp.Tween;

namespace Shared.Tp.Tests.Tween.Data
{
    public struct BasicUnmanaged
    {
        public int IntValue;
        public float FloatValue;

        public static BasicUnmanaged Make(int idx)
        {
            return new()
            {
                IntValue = 11 * (idx + 1),
                FloatValue = 11.1f * (idx + 1),
            };
        }

        public void AssertInRange(in BasicUnmanaged a, in BasicUnmanaged b)
        {
            Assert.That(IntValue, Is.InRange(a.IntValue, b.IntValue));
            Assert.That(FloatValue, Is.InRange(a.FloatValue, b.FloatValue));
        }
    }
    
    public class CustomUnmanagedBasicTweener : ITweener<BasicUnmanaged>
    {
        public void Process(ref BasicUnmanaged a, ref BasicUnmanaged b, float t, ref BasicUnmanaged r)
        {
            r.IntValue = (int)(a.IntValue * (1 - t) + b.IntValue * t);
            r.FloatValue = a.FloatValue * (1 - t) + b.FloatValue * t;
        }
    }
}