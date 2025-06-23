using Shared.Log;
using Shared.Tp.Ext.Misc;
using Shared.Tp.Util.Stat;
using UnityEngine;

namespace Client.Logic
{
    public class ClientTimeContext : ITimeContext
    {
        private readonly TimeLink _timeLink;
        private readonly TimeLink.Options _options;

        private TickPoint _remotePoint;
        private TickPoint _historyPoint;
        private CycleSampleSet _historyDeltaDeviationSet;

        public ClientTimeContext(TimeLink timeLink, TimeLink.Options options)
        {
            _timeLink = timeLink;
            _options = options;

            Update();
        }

        public void Update()
        {
            _remotePoint = new(_timeLink.RemoteClock.Count);
            var desireHistoryPoint = _remotePoint - TickPoint.FromMs(_options.HistoryOffsetMs); //TODO: use offset based on current smoothed server's send rate and rtt

            if (_historyPoint.Ticks != 0)
            {
                var desireDelta = desireHistoryPoint - _historyPoint;
                var desireDeltaTicks = desireDelta.Ticks;

                var unityDeltaSeconds = Time.unscaledDeltaTime;
                var unityDeltaTicks = (long)(unityDeltaSeconds * Ticker.TicksPerSeconds);
                var deltaDeviationTicks = desireDeltaTicks - unityDeltaTicks;

                _historyDeltaDeviationSet.Add((int)deltaDeviationTicks);

                //var historyDeltaTicks = desireDeltaTicks;
                var historyDeltaTicks = unityDeltaTicks + deltaDeviationTicks / 10; //XXXXXXX

                _historyPoint += new TickPoint(historyDeltaTicks);

                if (_options.LogHistory)
                {
                    // ReSharper disable once ExplicitCallerInfoArgument
                    Slog.Info($"DELTA-TICKS: {desireDeltaTicks,6} - {unityDeltaTicks,6} = {deltaDeviationTicks,6} ({_historyDeltaDeviationSet.MeanInt,7} ± {(int)_historyDeltaDeviationSet.StdDeviation,-6})", string.Empty, string.Empty);

                    // var remoteDeltaSeconds = remoteDelta.Seconds;
                    // Slog.Info($"DELTA-SECONDS: |{remoteDeltaSeconds:F7} - {unityDeltaSeconds:F7}| = {Mathf.Abs(remoteDeltaSeconds - unityDeltaSeconds):F7}");
                }
            }
            else
            {
                _historyPoint = desireHistoryPoint;
            }
        }

        TickPoint ITimeContext.NowTickPoint => _remotePoint;
        TickPoint ITimeContext.HistoryTickPoint => _historyPoint;
    }
}