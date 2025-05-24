using System.Runtime.InteropServices;
using NUnit.Framework;
using Shared.Sys.Rtt;
using Shared.Tp.Tween;

namespace Shared.Tp.Tests.Tween.Data
{
    [StructLayout(LayoutKind.Sequential)]
    public struct BasicStruct
    {
        [Tween] public int IntValue;
        [Tween] public string StringValue; // Makes struct managed
        public long LongValue; // NO tween

        public static BasicStruct Make(int idx)
        {
            return new()
            {
                IntValue = 11 * (idx + 1),
                StringValue = $"std:{idx}"
            };
        }

        public void AssertInRange(in BasicStruct a, in BasicStruct b)
        {
            Assert.That(IntValue, Is.InRange(a.IntValue, b.IntValue));
            Assert.That(StringValue, Is
                .EqualTo(a.StringValue).Or
                .EqualTo(b.StringValue));
            Assert.That(LongValue, Is.EqualTo(b.LongValue));
        }

        [UnityEngine.Scripting.Preserve] // ReSharper disable once UnusedMember.Global // TODO: auto-create via Shared.Sys.SourceGen
        public RttInfo GetRttInfo() => new RttInfo()
            .Add(ref this, ref IntValue, nameof(IntValue))
            .Add(ref this, ref StringValue, nameof(StringValue))
            .Add(ref this, ref LongValue, nameof(LongValue))
            ;
    }
}