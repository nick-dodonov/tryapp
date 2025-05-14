#if !UNITY_5_6_OR_NEWER
using System;
using NUnit.Framework;
using NUnit.Framework.Constraints;

// ReSharper disable once CheckNamespace
namespace UnityEngine.TestTools.Constraints
{
    /// <summary>
    /// Re-implemented UnityEngine.TestTools.Constraints.AllocatingGCMemoryConstraint for .NET core
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public class AllocatingGCMemoryConstraint : Constraint
    {
        // ReSharper disable once InconsistentNaming
        private class AllocatingGCMemoryResult : ConstraintResult
        {
            private readonly int _diff;
            public AllocatingGCMemoryResult(IConstraint constraint, object actualValue, int diff) 
                : base(constraint, actualValue, diff > 0)
            {
                _diff = diff;
            }

            public override void WriteMessageTo(MessageWriter writer)
            {
                if (_diff == 0)
                    writer.WriteMessageLine("The provided delegate did not make any GC allocations.");
                else
                    writer.WriteMessageLine("The provided delegate made {0} GC allocation(s).", _diff);
            }
        }

        public override ConstraintResult ApplyTo<TActual>(TActual actual)
        {
            if (actual == null)
                throw new ArgumentNullException(nameof(actual));

            if (actual is not TestDelegate del)
                throw new ArgumentException($"The actual value must be a TestDelegate but was {actual.GetType()}");

            int delta;
            var beforeThread = GC.GetAllocatedBytesForCurrentThread();
            try
            {
                del();
            }
            finally
            {
                var afterThread = GC.GetAllocatedBytesForCurrentThread();
                delta = (int)(afterThread - beforeThread);
            }

            return new AllocatingGCMemoryResult(this, actual, delta);
        }
        
        public override string Description => "allocates GC memory";
    }
}
#endif