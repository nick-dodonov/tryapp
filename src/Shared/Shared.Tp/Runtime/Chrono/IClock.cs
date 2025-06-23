namespace Shared.Tp.Chrono
{
    public interface IClock<out T, TPeriod> where T
        : unmanaged
        where TPeriod : IPeriod, new()
    {
        public static readonly long CountPerSec = new TPeriod().InstanceCountPerSec;

        T Count { get; }
    }
}