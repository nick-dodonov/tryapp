using NUnit.Framework;
using Shared.Tp.Tween;

namespace Shared.Tp.Tests.Tween.Data
{
    public struct UnmanagedBasic
    {
        public int IntValue;
        public float FloatValue;

        public static UnmanagedBasic Make(int idx)
        {
            return new()
            {
                IntValue = 11 * (idx + 1),
                FloatValue = 11.1f * (idx + 1),
            };
        }

        public void AssertInRange(in UnmanagedBasic a, in UnmanagedBasic b)
        {
            Assert.That(IntValue, Is.InRange(a.IntValue, b.IntValue));
            Assert.That(FloatValue, Is.InRange(a.FloatValue, b.FloatValue));
        }
    }
    
    public class CustomUnmanagedBasicTweener : ITweener<UnmanagedBasic>
    {
        public void Process(ref UnmanagedBasic a, ref UnmanagedBasic b, float t, ref UnmanagedBasic r)
        {
            r.IntValue = (int)(a.IntValue * (1 - t) + b.IntValue * t);
            r.FloatValue = a.FloatValue * (1 - t) + b.FloatValue * t;
        }
    }
}