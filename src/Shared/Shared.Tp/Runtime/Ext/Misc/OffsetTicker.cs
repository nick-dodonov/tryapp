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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetOffset(long offset) => _offset = offset;
    }
}