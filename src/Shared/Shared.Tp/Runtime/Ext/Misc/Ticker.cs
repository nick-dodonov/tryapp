using System.Runtime.CompilerServices;
using Shared.Tp.Chrono;

namespace Shared.Tp.Ext.Misc
{
    public readonly struct Ticker
    {
        public const int RtPerMs = 10;
        public const long RtPerSec = RtPerMs * 1000;

        public const ushort MaxCycledRt = 0xFFFF;

        public static readonly long TicksPerSeconds = RawPeriod.CountPerSec;
        public static readonly long TicksPerMs = TicksPerSeconds / 1000;
        public static readonly long TicksPerRt = TicksPerMs / RtPerMs;
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
