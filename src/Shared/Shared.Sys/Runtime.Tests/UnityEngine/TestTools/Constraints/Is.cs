#if !UNITY_5_6_OR_NEWER
// ReSharper disable once CheckNamespace
namespace UnityEngine.TestTools.Constraints
{
    /// <summary>
    /// Re-implemented UnityEngine.TestTools.Constraints.Is.AllocatingGCMemory() for .NET core
    /// 
    /// TODO: possibly make as Shared.Sys.Tests.Is with .NET and Unity impls
    /// 
    /// </summary>
    // ReSharper disable once ClassNeverInstantiated.Global
    public class Is : NUnit.Framework.Is
    {
        // ReSharper disable once InconsistentNaming
        public static AllocatingGCMemoryConstraint AllocatingGCMemory()
        {
            return new();
        }
    }
}
#endif