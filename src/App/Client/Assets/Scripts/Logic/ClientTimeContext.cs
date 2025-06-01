using Shared.Tp.Ext.Misc;
using UnityEngine;

namespace Client.Logic
{
    public class ClientTimeContext : ITimeContext
    {
        private readonly TimeLink _timeLink;
        private readonly ClientTimeOptions _options;

        private ushort _frameCycledRt;
        
        private float _frameCycledRtFraction;

        public ClientTimeContext(TimeLink timeLink, ClientTimeOptions options)
        {
            _timeLink = timeLink;
            _options = options;

            Update();
        }

        public void Update()
        {
            var remoteTicker = _timeLink.RemoteTicker;

            var ticks = remoteTicker.Ticks;
            var rt = ticks / Ticker.TicksPerRt;
            
            //var deltaRt = (ushort)(rt - _frameCycledRt);
            _frameCycledRt = (ushort)(rt & Ticker.MaxCycledRt);

            var roundedTicks = rt * Ticker.TicksPerRt;
            var remainTicks = ticks - roundedTicks;
            _frameCycledRtFraction = (float)remainTicks / Ticker.TicksPerRt;

            // var remoteDeltaTime = (float)deltaRt / Ticker.RtPerSec;
            // Shared.Log.Slog.Info($"{Time.frameCount}: {Time.deltaTime} - {remoteDeltaTime} - {_frameCycledRt} - {_frameCycledRtFraction}");
        }

        //TODO: use start of frame time point instead of instant value
        ushort ITimeContext.CurrentSessionCycledRt => _frameCycledRt;

        //TODO: use offset based on current smoothed server's send rate and rtt
        ushort ITimeContext.HistorySessionCycledRt => (ushort)(_frameCycledRt - _options.HistoryOffsetMs * Ticker.RtPerMs);
        float ITimeContext.HistorySessionCycledRtFraction => _frameCycledRtFraction;
    }
}