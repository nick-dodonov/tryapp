namespace Shared.Tp.St.Sync
{
    public struct StCmd<T>
    {
        //TODO: compress in formatter
        public int From; // local frame
        public int To; // local frame
        public int Known; // remote frame

        public int Ms; // time (local on server, remote on client)
        
        //TODO: replace with diff
        public T Value;
    }
}