using System.Runtime.CompilerServices;

namespace Shared.Tp.Ext.Misc
{
    /// <summary>
    /// NOTE: Unfortunately, either Stopwatch.GetTimestamp() or DateTime.UtcNow.Ticks doesn't give
    ///     good accuracy on WebGL (1 ms in Unity 6.1.4 build from logs of TickerTests.Delta_Impls).
    ///     It can be the reason for very small interpolation lags on high movement speed of objects in the browser.
    ///
    /// TODO: Check UnityEngine.Time.unscaledTime accuracy, research ticker can be re-implemented on js 
    ///  
    /// </summary>
    public readonly struct Ticker
    {
        public const long RtPerMs = 10; // (1/10 ms | 100 mk) run-tick is the selected time accuracy for networking
        public const long RtPerSec = RtPerMs * 1000;

        /// <summary>
        /// CycleRt is a "packed" absolute time value. Required to synchronize remote sides.
        /// With rt as 1/10 ms:
        /// - 0xFFFF ~6.5 sec
        /// - 0xF_FFFF ~105 sec
        /// - 0xFF_FFFF ~27 min
        /// </summary>
        private const int MaxCycleRt = 0xFFFF; // TODO: make 0xFF_FFFF after logic stabilization

        // //DATETIME:
        // private static long NowTicks
        // {
        //     [MethodImpl(MethodImplOptions.AggressiveInlining)]
        //     get => System.DateTime.UtcNow.Ticks;
        // }
        // public const long TicksPerSeconds = System.TimeSpan.TicksPerSecond;

        //STOPWATCH:
        private static long NowTicks
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => System.Diagnostics.Stopwatch.GetTimestamp();
        }
        public static readonly long TicksPerSeconds = System.Diagnostics.Stopwatch.Frequency;

        public static readonly long TicksPerMs = TicksPerSeconds / 1000;
        public static readonly long TicksPerRt = TicksPerMs / RtPerMs;

        private readonly long _startTicks;

        public static Ticker StartNew() => new(NowTicks);

        private Ticker(long startTicks) => _startTicks = startTicks;

        public long StartTicks
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _startTicks;
        }

        public long Ticks
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => NowTicks - _startTicks;
        }

        public long Rt
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Ticks / TicksPerRt;
        }

        public int CycleRt
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (int)(Ticks / TicksPerRt & MaxCycleRt);
        }

        public int Ms
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (int)(Ticks / TicksPerMs);
        }

        public readonly struct Point
        {
            private readonly long _value;
        }
    }
}
