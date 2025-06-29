using Shared.Log;
using Shared.Tp.Chrono;
using Shared.Tp.Ext.Misc;
using Shared.Tp.Util.Stat;
using UnityEngine;

namespace Client.Logic
{
    using RemoteClock = DeltaClock<long, RawPeriod, RawClock>;
    
    public class ClientTimeContext : ITimeContext
    {
        private readonly TimeLink _timeLink;
        private readonly TimeLink.Options _options;

        private Tick<long, RawPeriod> _remoteTick;
        private Tick<long, RawPeriod> _historyTick;

        //private FrameClock<RawPeriod, RemoteClock> _remoteFrameClock;
        
        private CycleSampleSet _historyDeltaDeviationSet;

        public ClientTimeContext(TimeLink timeLink, TimeLink.Options options)
        {
            _timeLink = timeLink;
            _options = options;

            Update();
        }

        public void Update()
        {
            _remoteTick = _timeLink.RemoteClock.Tick();
            var desireHistoryCount = _remoteTick.Count - _options.HistoryOffsetMs * RawPeriod.CountPerMs;

            if (_historyTick.Count != 0)
            {
                var desireDeltaTicks = desireHistoryCount - _historyTick.Count;

                var unityDeltaSeconds = Time.unscaledDeltaTime;
                var unityDeltaTicks = (long)(unityDeltaSeconds * RawPeriod.CountPerSec);
                var deltaDeviationTicks = desireDeltaTicks - unityDeltaTicks;

                _historyDeltaDeviationSet.Add((int)deltaDeviationTicks);

                //var historyDeltaTicks = desireDeltaTicks;
                var historyDeltaTicks = unityDeltaTicks + deltaDeviationTicks / 10; //XXXXXXX

                _historyTick = new(_historyTick.Count + historyDeltaTicks);

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
                _historyTick = new(desireHistoryCount);
            }
        }

        Tick<long, RawPeriod> ITimeContext.NowTick => _remoteTick;
        Tick<long, RawPeriod> ITimeContext.HistoryTick => _historyTick;
    }
}