using System.Runtime.CompilerServices;

namespace Shared.Tp.Tick
{
    public interface IClock<out T, TPeriod> where T 
        : unmanaged where TPeriod : IPeriod, new()
    {
        public static readonly TPeriod Period = new();

        T Count { get; }
    }

    public readonly struct Clock<T, TSourcePeriod, TSourceClock> : IClock<T, TSourcePeriod>
        where T : unmanaged
        where TSourcePeriod : IPeriod, new()
        where TSourceClock : IClock<T, TSourcePeriod>
    {
        public static long CountPerSec => new TSourcePeriod().InstanceCountPerSec;

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
}