namespace Shared.Tp.St.Sync
{
    public struct StCmd<T>
    {
        //TODO: compress in formatter
        public int From; // local
        public int To; // local
        public int Known; // remote

        //TODO: replace with diff
        public T Value;
    }
}