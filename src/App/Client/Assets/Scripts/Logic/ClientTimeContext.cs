using Shared.Tp.Ext.Misc;

namespace Client.Logic
{
    public class ClientTimeContext : ITimeContext
    {
        private readonly TimeLink _timeLink;
        private readonly ClientTimeOptions _options;

        private TickPoint _remoteTickPoint;

        public ClientTimeContext(TimeLink timeLink, ClientTimeOptions options)
        {
            _timeLink = timeLink;
            _options = options;

            Update();
        }

        public void Update()
        {
            _remoteTickPoint = _timeLink.RemoteTicker.Point;
        }

        TickPoint ITimeContext.NowTickPoint => _remoteTickPoint;

        //TODO: use offset based on current smoothed server's send rate and rtt
        TickPoint ITimeContext.HistoryTickPoint => _remoteTickPoint - TickPoint.FromMs(_options.HistoryOffsetMs);
    }
}