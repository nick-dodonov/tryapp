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
        int ITimeContext.CurrentSessionMs => _timeLink.RemoteMs;

        //TODO: use offset based on current smoothed server's send rate and rtt
        int ITimeContext.HistorySessionMs => _timeLink.RemoteMs - 210;
        int ITimeContext.HistoryCycleRt
        {
            get
            {
                var value = _timeLink.RemoteTicker.CycleRt - 210 * Ticker.RtPerMs;
                if (value < 0)
                    value += Ticker.MaxCycleRt;
                return value;
            }
        }
    }
}