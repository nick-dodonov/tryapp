namespace Shared.Tp.St.Sync
{
    public class StHistory<T> : History<int, T>
    {
        public StHistory(int initCapacity) : base(initCapacity) { }
    }
}