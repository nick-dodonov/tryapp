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

        //TODO: currentMs using smoothed rtt
        int ITimeContext.CurrentSessionMs => _timeLink.RemoteMs;
        
        //TODO: use frame start session ms instead
        //TODO: constant based on current server's send rate
        int ITimeContext.HistorySessionMs => _timeLink.RemoteMs - 210;
    }
}