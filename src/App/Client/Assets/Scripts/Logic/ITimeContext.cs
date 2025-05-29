namespace Client.Logic
{
    public interface ITimeContext
    {
        public ushort CurrentSessionCycledRt { get; }
        public ushort HistorySessionCycledRt { get; }
    }
}