using Shared.Tp.Ext.Misc;

namespace Client.Logic
{
    public interface ITimeContext
    {
        public TickPoint NowTickPoint { get; }
        public TickPoint HistoryTickPoint { get; }
    }
}