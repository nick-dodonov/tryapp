using System.Runtime.InteropServices;
using NUnit.Framework;
using Shared.Sys.Rtt;
using Shared.Tp.Tween;

namespace Shared.Tp.Tests.Tween.Data
{
    public struct NestedStruct
    {
        public long LongValue; // NO tween
        [Tween] public BasicUnmanaged BasicUnmanaged;
        [Tween] public BasicStruct BasicStruct;

        public static NestedStruct Make(int idx)
        {
            return new()
            {
                LongValue = 111 * (idx + 1),
                BasicUnmanaged = BasicUnmanaged.Make(idx),
                BasicStruct = BasicStruct.Make(idx)
            };
        }

        public void AssertInRange(in NestedStruct a, in NestedStruct b)
        {
            Assert.That(LongValue, Is.EqualTo(b.LongValue));
            BasicUnmanaged.AssertInRange(a.BasicUnmanaged, b.BasicUnmanaged);
            BasicStruct.AssertInRange(a.BasicStruct, b.BasicStruct);
        }

        [UnityEngine.Scripting.Preserve] // ReSharper disable once UnusedMember.Global // TODO: auto-create via Shared.Sys.SourceGen
        public RttInfo GetRttInfo() => new RttInfo()
            .Add(ref this, ref LongValue, nameof(LongValue))
            .Add(ref this, ref BasicUnmanaged, nameof(BasicUnmanaged))
            .Add(ref this, ref BasicStruct, nameof(BasicStruct))
            ;
    }
}