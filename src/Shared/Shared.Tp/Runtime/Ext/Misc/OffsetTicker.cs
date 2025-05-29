using System.Runtime.CompilerServices;

namespace Shared.Tp.Ext.Misc
{
    public struct OffsetTicker
    {
        private readonly Ticker _ticker;
        private long _offset;

        public OffsetTicker(Ticker ticker)
        {
            _ticker = ticker;
            _offset = 0;
        }

        public long Ticks
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _ticker.Ticks + _offset;
        }
        
        public long Rt
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Ticks / Ticker.TicksPerRt;
        }

        public int CycleRt
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (int)(Ticks / Ticker.TicksPerRt & Ticker.MaxCycleRt);
        }
        
        public int Ms
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (int)(Ticks / Ticker.TicksPerMs);
        }

        public float Seconds
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Ticks / (float)Ticker.TicksPerSeconds;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetOffset(long offset) => _offset = offset;
    }
}