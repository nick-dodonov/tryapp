using Shared.Log;
using Shared.Tp.Ext.Misc;
using UnityEngine;

namespace Client.Logic
{
    public class ClientTimeContext : ITimeContext
    {
        private readonly TimeLink _timeLink;
        private readonly ClientTimeOptions _options;

        private TickPoint _remotePoint;
        private TickPoint _historyPoint;
        private float _historyDeltaSeconds;
        private float _historyDeltaSecondsSpeed;

        public ClientTimeContext(TimeLink timeLink, ClientTimeOptions options)
        {
            _timeLink = timeLink;
            _options = options;

            Update();
        }

        public void Update()
        {
            _remotePoint = _timeLink.RemoteTicker.Point;

            //TODO: use offset based on current smoothed server's send rate and rtt
            var previousPoint = _historyPoint;
            _historyPoint = _remotePoint - TickPoint.FromMs(_options.HistoryOffsetMs);

            if (previousPoint.Ticks != 0)
            {
                var remoteDelta = _historyPoint - previousPoint;
                var remoteDeltaTicks = remoteDelta.Ticks;

                var unityDeltaSeconds = Time.unscaledDeltaTime;
                var unityDeltaTicks = (long)(unityDeltaSeconds * Ticker.TicksPerSeconds);
                //Slog.Info($"DELTA-TICKS: |{remoteDeltaTicks,6} - {unityDeltaTicks,6}| = {remoteDeltaTicks - unityDeltaTicks,5} / {Ticker.TicksPerMs}");

                var remoteDeltaSeconds = remoteDelta.Seconds;
                Slog.Info($"DELTA-SECONDS: |{remoteDeltaSeconds:F7} - {unityDeltaSeconds:F7}| = {Mathf.Abs(remoteDeltaSeconds - unityDeltaSeconds):F7}");
                
                _historyDeltaSeconds = Mathf.SmoothDamp(
                    _historyDeltaSeconds, remoteDeltaSeconds, 
                    ref _historyDeltaSecondsSpeed, 0.1f);
                var historyDeltaTicks = (long)(_historyDeltaSeconds * Ticker.TicksPerSeconds);
                _historyPoint = previousPoint + new TickPoint(historyDeltaTicks);
                
                Slog.Info($"DELTA-REAL-TICKS: |{historyDeltaTicks,6} - {unityDeltaTicks,6}| = {historyDeltaTicks - unityDeltaTicks,6} / {Ticker.TicksPerMs}");
            }
            else
            {
                _historyDeltaSeconds = Time.unscaledDeltaTime;
            }
        }

        TickPoint ITimeContext.NowTickPoint => _remotePoint;
        TickPoint ITimeContext.HistoryTickPoint => _historyPoint;
    }
}