namespace Client.Logic
{
    public interface ITimeContext
    {
        public int CurrentSessionMs { get; }
        public int HistorySessionMs { get; }
    }
}