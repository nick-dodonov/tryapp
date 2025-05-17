using System.Runtime.InteropServices;
using NUnit.Framework;
using Shared.Sys.Rtt;

namespace Shared.Tp.Tests.Tween.Data
{
    [StructLayout(LayoutKind.Sequential)]
    public struct ManagedBasic
    {
        public int IntValue;
        public string StringValue;

        public static ManagedBasic Make(int idx)
        {
            return new()
            {
                IntValue = 11 * (idx + 1),
                StringValue = $"std:{idx}"
            };
        }

        public void AssertInRange(in ManagedBasic a, in ManagedBasic b)
        {
            Assert.That(IntValue, Is.InRange(a.IntValue, b.IntValue));
            Assert.That(StringValue, Is
                .EqualTo(a.StringValue).Or
                .EqualTo(b.StringValue));
        }

        [UnityEngine.Scripting.Preserve] // ReSharper disable once UnusedMember.Global // TODO: auto-create via Shared.Sys.SourceGen
        public RttInfo GetRttInfo() => new RttInfo()
            .Add(ref this, ref IntValue, nameof(IntValue))
            .Add(ref this, ref StringValue, nameof(StringValue))
            ;
    }
}