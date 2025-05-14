using NUnit.Framework;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace Shared.Tp.Tests.Tween
{
    public struct TestUnmanaged
    {
        public int I;
        public float F;

        public static TestUnmanaged Make(int idx)
        {
            return new()
            {
                I = 11 * (idx + 1),
                F = 11.1f * (idx + 1),
            };
        }

        public static void CustomTween(ref TestUnmanaged v0, ref TestUnmanaged v1, float t, ref TestUnmanaged vr)
        {
            vr.I = (int)(v0.I * (1 - t) + v1.I * t);
            vr.F = v0.F * (1 - t) + v1.F * t;
        }
    }

    public class TweenTests
    {
        [Test]
        public void CustomTweenUnmanaged_NoAllocations()
        {
            var v0 = TestUnmanaged.Make(0);
            var v1 = TestUnmanaged.Make(1);
            var vr = TestUnmanaged.Make(1);

            Assert.That(() => {
                TestUnmanaged.CustomTween(ref v0, ref v1, 0.5f, ref vr);
            }, Is.Not.AllocatingGCMemory());

            Assert.That(vr.I, Is.InRange(v0.I, v1.I));
            Assert.That(vr.F, Is.InRange(v0.F, v1.F));
        }
    }
}