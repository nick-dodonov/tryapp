using System.Diagnostics.CodeAnalysis;
using NUnit.Framework;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace Shared.Sys.Tests
{
    public struct TestStruct
    {
        public int IntValue;
        public float FloatValue;
    }

    [SuppressMessage("ReSharper", "AccessToStaticMemberViaDerivedType")]
    public class AllocatingGcMemoryTests
    {
        [Test]
        public void ActionInvokeGcFree()
        {
            var action = new System.Action(() => { });
            Assert.That(() => {
                action.Invoke();
            }, Is.Not.AllocatingGCMemory());            
        }
        
        [Test]
        public void StructBoxingAllocates()
        {
            Assert.That(() => {
                var val = new TestStruct();
                var obj = (object)val;
                Assert.NotNull(obj);
            }, Is.AllocatingGCMemory());            
        }
    }
}