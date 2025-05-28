namespace Shared.Tp.St.Sync
{
    public class StHistory<T> : History<StKey, T>
    {
        public StHistory(int initCapacity) : base(initCapacity) { }
    }
}