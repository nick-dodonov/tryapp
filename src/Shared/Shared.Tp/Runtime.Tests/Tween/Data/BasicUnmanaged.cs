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
    
    public class CustomBasicUnmanagedTweener : ITweener<BasicUnmanaged>
    {
        public void Replica(ref BasicUnmanaged dst, in BasicUnmanaged src) => dst = src;
        public void Process(ref BasicUnmanaged dst, float t, in BasicUnmanaged src0, in BasicUnmanaged src1)
        {
            dst.IntValue = (int)(src0.IntValue * (1 - t) + src1.IntValue * t);
            dst.FloatValue = src0.FloatValue * (1 - t) + src1.FloatValue * t;
        }
    }
}