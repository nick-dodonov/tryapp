#if !UNITY_5_6_OR_NEWER
using NUnit.Framework.Constraints;

// ReSharper disable once CheckNamespace
namespace UnityEngine.TestTools.Constraints
{
    /// <summary>
    /// Re-implemented UnityEngine.TestTools.Constraints.ConstraintExtensions for .NET core
    /// </summary>
    public static class ConstraintExtensions
    {
        // ReSharper disable once InconsistentNaming
        public static AllocatingGCMemoryConstraint AllocatingGCMemory(this ConstraintExpression chain)
        {
            var constraint = new AllocatingGCMemoryConstraint();
            chain.Append(constraint);
            return constraint;
        }
    }
}
#endif