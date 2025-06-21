using System.Runtime.CompilerServices;
using Shared.Tp.Tick;

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
        public const int RtPerMs = 10; // (1/10 ms | 100 mk) run-tick is the selected time accuracy for networking
        public const long RtPerSec = RtPerMs * 1000;

        /// <summary>
        /// CycledRt is a "packed" absolute time value. Used to synchronize remote sides.
        /// Events to associate cannot have a time interval more than max value (re-sync happens in this case).
        /// 
        /// With Rt as 1/10 ms:
        /// - 0xFFFF ~6.5 sec
        /// - 0xF_FFFF ~105 sec
        /// - 0xFF_FFFF ~27 min
        /// - 0xFFF_FFFF ~7.5 hours
        /// - 0xFFFF_FFFF ~5 days
        /// </summary>
        public const ushort MaxCycledRt = 0xFFFF;

        private static readonly Clock<long, SystemClock> _clock = new(new());
        public static readonly long TicksPerSeconds = _clock.CountPerSec;
        private static long NowTicks
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _clock.Count;
        }

        public static readonly long TicksPerMs = TicksPerSeconds / 1000;
        public static readonly long TicksPerRt = TicksPerMs / RtPerMs;

        private readonly long _startTicks;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Ticker StartNew(long offsetTicks = 0) => new(NowTicks - offsetTicks);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

        public TickPoint Point
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new(Ticks);
        }
    }

    public readonly struct TickPoint
    {
        private readonly long _ticks;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TickPoint(long ticks) => _ticks = ticks;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TickPoint FromMs(long ms) => new(ms * Ticker.TicksPerMs);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TickPoint operator -(TickPoint a, TickPoint b) => new(a._ticks - b._ticks);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TickPoint operator +(TickPoint a, TickPoint b) => new(a._ticks + b._ticks);

        public long Ticks
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _ticks;
        }
        
        public long Rt
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _ticks / Ticker.TicksPerRt;
        }

        public float RtFraction
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _ticks % Ticker.TicksPerRt / (float)Ticker.TicksPerRt;
        }

        public float Seconds
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _ticks / (float)Ticker.TicksPerSeconds;
        }

        public ushort CycledRt
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (ushort)(_ticks / Ticker.TicksPerRt & Ticker.MaxCycledRt);
        }

        public float CycledSeconds
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (CycledRt + RtFraction) / Ticker.RtPerSec;
        }
    }
}
