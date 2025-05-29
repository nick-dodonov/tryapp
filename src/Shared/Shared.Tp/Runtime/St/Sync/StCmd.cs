namespace Shared.Tp.St.Sync
{
    public struct StCmd<T>
    {
        //TODO: compress in formatter
        public int From; // local frame
        public int To; // local frame
        public int Known; // remote frame

        public ushort CycledRt; // time (local on the server, remote on the client)

        //TODO: replace with diff
        public T Value;
    }
}