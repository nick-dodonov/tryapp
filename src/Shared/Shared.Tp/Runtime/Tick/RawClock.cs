using System.Runtime.CompilerServices;

namespace Shared.Tp.Tick
{
    public readonly struct RawPeriod : IPeriod
    {
        //STOPWATCH:
        public static readonly long CountPerSec = System.Diagnostics.Stopwatch.Frequency;

        // //DATETIME:
        // public static readonly long CountPerSec = System.Diagnostics.Stopwatch.Frequency;

        public long InstanceCountPerSec
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => CountPerSec;
        }
    }

    public readonly struct RawClock : IClock<long, RawPeriod>
    {
        public static readonly long CountPerSec = RawPeriod.CountPerSec;
        public static RawClock Instance = new();

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