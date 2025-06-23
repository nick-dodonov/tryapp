using System.Runtime.CompilerServices;

namespace Shared.Tp.Chrono
{
    public readonly struct Tick<T, TPeriod>
        where T : unmanaged
        where TPeriod : IPeriod, new()
    {
        public static readonly long CountPerSec = new TPeriod().InstanceCountPerSec;

        private readonly T _count;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Tick(T count) => _count = count;

        public T Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _count;
        }
    }

    public static class TickExtensions
    {
        public static float Seconds<TPeriod>(this Tick<long, TPeriod> tick)
            where TPeriod : IPeriod, new()
            => tick.Count / (float)Tick<long, TPeriod>.CountPerSec;

        public static float Seconds<TPeriod>(this Tick<ushort, TPeriod> tick)
            where TPeriod : IPeriod, new()
            => tick.Count / (float)Tick<long, TPeriod>.CountPerSec;
    }
}