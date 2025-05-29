using Shared.Tp.Ext.Misc;

namespace Client.Logic
{
    public class ClientTimeContext : ITimeContext
    {
        private readonly TimeLink _timeLink;
        public ClientTimeContext(TimeLink timeLink)
        {
            _timeLink = timeLink;
        }

        //TODO: use start of frame time point instead of instant value
        ushort ITimeContext.CurrentSessionCycledRt => _timeLink.RemoteTicker.CycledRt;

        //TODO: use offset based on current smoothed server's send rate and rtt
        ushort ITimeContext.HistorySessionCycledRt => (ushort)(_timeLink.RemoteTicker.CycledRt - 220 * Ticker.RtPerMs);
    }
}