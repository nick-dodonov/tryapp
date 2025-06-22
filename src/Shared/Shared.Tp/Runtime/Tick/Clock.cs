using System.Runtime.CompilerServices;

namespace Shared.Tp.Tick
{
    public interface IPeriod
    {
        public long InstanceCountPerSec { get; }
    }

    public interface IClock<out T, TPeriod> where T 
        : unmanaged where TPeriod : IPeriod, new()
    {
        public static readonly TPeriod Period = new();

        T Count { get; }
    }

    public readonly struct Clock<T, TPeriod, TSourceClock> : IClock<T, TPeriod>
        where T : unmanaged
        where TPeriod : IPeriod, new()
        where TSourceClock : IClock<T, TPeriod>
    {
        public static long CountPerSec => new TPeriod().InstanceCountPerSec;

        private readonly TSourceClock _sourceClock;
        private readonly T _startCount;

        public Clock(TSourceClock sourceClock)
        {
            _sourceClock = sourceClock;
            _startCount = sourceClock.Count;
        }

        public Clock(TSourceClock sourceClock, T sourceOffset)
        {
            _sourceClock = sourceClock;
            _startCount = NumericHelper.Add(_sourceClock.Count, sourceOffset);
        }

        public T StartCount => _startCount;

        public T Count => NumericHelper.Sub(_sourceClock.Count, _startCount);
    }

    public readonly struct Tick<T, TPeriod> 
        where T : unmanaged 
        where TPeriod : IPeriod, new()
    {
        public static readonly TPeriod Period = new();
        private readonly T _count;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Tick(T count) => _count = count;
        
        public T Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _count;
        }
    }

    public static class ClockExtensions
    {
        public static Tick<T, TPeriod> Tick<T, TPeriod>(this IClock<T, TPeriod> clock) 
            where T : unmanaged 
            where TPeriod : IPeriod, new()
            => new(clock.Count);
    }

    public static class TickExtensions
    {
        // public static Tick<ushort, RtClock> ToCycled(this Tick<long, RtClock> point, ushort maxCount = ushort.MaxValue) => 
        //     new((ushort)(point.Count % (maxCount + 1)));
    }
}