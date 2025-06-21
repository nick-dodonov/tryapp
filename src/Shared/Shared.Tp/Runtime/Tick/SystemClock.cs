using System.Runtime.CompilerServices;

namespace Shared.Tp.Tick
{
    public readonly struct SystemClock : IClock<long>
    {
        //STOPWATCH:
        public long Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => System.Diagnostics.Stopwatch.GetTimestamp();
        }

        public long CountPerSec
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => System.Diagnostics.Stopwatch.Frequency;
        }

        // //DATETIME:
        // public long Count
        // {
        //     [MethodImpl(MethodImplOptions.AggressiveInlining)]
        //     get => System.DateTime.UtcNow.Ticks;
        // }
        //
        // public long CountPerSec
        // {
        //     [MethodImpl(MethodImplOptions.AggressiveInlining)]
        //     get => System.TimeSpan.TicksPerSecond;
        // }
    }
}