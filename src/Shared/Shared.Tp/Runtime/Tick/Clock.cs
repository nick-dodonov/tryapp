using System.Runtime.CompilerServices;

namespace Shared.Tp.Tick
{
    public interface IClock<out T> where T : unmanaged
    {
        T Count { get; }
        public long CountPerSec { get; }
    }

    public readonly struct Clock<T, TSourceClock> : IClock<T>
        where TSourceClock : IClock<T>
        where T : unmanaged
    {
        private readonly TSourceClock _sourceClock;
        private readonly T _startCount;

        public Clock(TSourceClock sourceClock)
        {
            _sourceClock = sourceClock;
            _startCount = sourceClock.Count;
        }

        public T Count => NumericHelper.Sub(_sourceClock.Count, _startCount);
        public long CountPerSec => _sourceClock.CountPerSec;
    }

    public readonly struct ClockPoint<T> where T : unmanaged
    {
        private readonly T _count;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ClockPoint(T count) => _count = count;
        
        public T Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _count;
        }
    }

    public static class ClockExtensions
    {
        public static ClockPoint<T> ClockPoint<T>(this IClock<T> clock) 
            where T : unmanaged => 
            new(clock.Count);
    }

    public static class ClockPointExtensions
    {
        public static ClockPoint<ushort> ToCycled(this ClockPoint<long> point, ushort maxCount = ushort.MaxValue) => 
            new((ushort)(point.Count % (maxCount + 1)));
        
        public static float ToSeconds(this ClockPoint<long> point, long countPerSec) => (float)((double)point.Count / countPerSec);
        public static float ToSeconds(this ClockPoint<ushort> point, long countPerSec) => (float)point.Count / countPerSec;
    }
}