namespace Shared.Tp.St.Sync
{
    public class StHistory<T> : History<(int frame, int ms), T>
    {
        public StHistory(int initCapacity) : base(initCapacity) { }
    }
}