using System.Runtime.CompilerServices;

namespace Shared.Tp.Chrono
{
    /// <summary>
    /// NOTE: Unfortunately, either Stopwatch.GetTimestamp() or DateTime.UtcNow.Ticks doesn't give
    ///     good accuracy on WebGL (1 ms in Unity 6.1.4 build - from logs of TickerTests.Delta_Impls).
    ///     It can be the reason for very small interpolation lags on high movement speed of objects in the browser.
    ///
    /// TODO: Check UnityEngine.Time.unscaledTime accuracy, research ticker can be re-implemented on js 
    /// 
    /// </summary>
    public readonly struct RawPeriod : IPeriod
    {
        public static readonly long CountPerSec;
        public static readonly long CountPerMs;

        static RawPeriod()
        {
            //STOPWATCH:
            CountPerSec = System.Diagnostics.Stopwatch.Frequency;
            // //DATETIME:
            // CountPerSec = System.TimeSpan.TicksPerSecond;

            CountPerMs = CountPerSec / 1000;
        }

        public long InstanceCountPerSec
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => CountPerSec;
        }
    }

    public readonly struct RawClock : IClock<long, RawPeriod>
    {
        public static readonly long CountPerSec = RawPeriod.CountPerSec;

        //STOPWATCH:
        public long Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => System.Diagnostics.Stopwatch.GetTimestamp();
        }

        // //DATETIME:
        // public long Count
        // {
        //     [MethodImpl(MethodImplOptions.AggressiveInlining)]
        //     get => System.DateTime.UtcNow.Ticks;
        // }
    }
}