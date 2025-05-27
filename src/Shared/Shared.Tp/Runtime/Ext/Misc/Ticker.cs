using System;
using System.Runtime.CompilerServices;

namespace Shared.Tp.Ext.Misc
{
    public readonly struct Ticker
    {
        public const long RtPerMs = 10; // (1/10 ms | 100 mk) run-tick is the selected time accuracy for networking
        public const long RtPerSec = RtPerMs * 1000;
        public const long TicksPerRt = TimeSpan.TicksPerMillisecond / RtPerMs;
        public const long TicksPerMs = TimeSpan.TicksPerMillisecond;
        public const long TicksPerSeconds = TimeSpan.TicksPerSecond;

        /// <summary>
        /// CycleRt is a "packed" absolute time value. Required to synchronize remote sides.
        /// 
        /// With rt as 1/10 ms:
        /// * 0xFFFF ~6.5 sec
        /// * 0xF_FFFF ~105 sec
        /// * 0xFF_FFFF ~27 min
        /// 
        /// </summary>
        private const int MaxCycleRt = 0xFFFF; // TODO: make 0xFF_FFFF after logic stabilization

        private readonly long _startTicks;

        public static Ticker StartNew() => new(DateTime.UtcNow.Ticks);
        private Ticker(long startTicks) => _startTicks = startTicks;

        public long StartTicks
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _startTicks;
        }

        public long Ticks
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => DateTime.UtcNow.Ticks - _startTicks;
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
    }

    public struct OffsetTicker
    {
        private readonly Ticker _ticker;
        private long _offset;

        public OffsetTicker(Ticker ticker)
        {
            _ticker = ticker;
            _offset = 0;
        }

        public long Rt
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (_ticker.Ticks + _offset) / Ticker.TicksPerRt;
        }

        public int Ms
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (int)((_ticker.Ticks + _offset) / Ticker.TicksPerMs);
        }

        public float Seconds
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (_ticker.Ticks + _offset) / (float)Ticker.TicksPerSeconds;
        }

        public void SetOffset(long offset) => _offset = offset;
    }
}
