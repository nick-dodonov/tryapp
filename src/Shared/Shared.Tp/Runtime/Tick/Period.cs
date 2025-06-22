using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Unity.IL2CPP.CompilerServices;

namespace Shared.Tp.Tick
{
    public interface IPeriod
    {
        public long InstanceCountPerSec { get; }
    }

    [Il2CppSetOption(Option.DivideByZeroChecks, false)]
    public readonly struct PeriodConvert<TFromPeriod, TToPeriod>
        where TFromPeriod : IPeriod, new()
        where TToPeriod : IPeriod, new()
    {
        // ReSharper disable StaticMemberInGenericType
        private static readonly long SourceCountNum;
        private static readonly long SourceCountDen;
        // ReSharper restore StaticMemberInGenericType

        static PeriodConvert()
        {
            var fromCountPerSec = new TFromPeriod().InstanceCountPerSec;
            var toCountPerSec = new TToPeriod().InstanceCountPerSec;
            Debug.Assert(fromCountPerSec > 0 && toCountPerSec > 0);
            SourceCountNum = Math.Max(1, toCountPerSec / fromCountPerSec);
            SourceCountDen = Math.Max(1, fromCountPerSec / toCountPerSec);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long Convert(long fromCount) => SourceCountNum * fromCount / SourceCountDen;
    }
}