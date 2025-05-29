using Shared.Log;
using Shared.Tp.Ext.Misc;
using UnityEngine;

namespace Client.Logic
{
    public class ClientTimeContext : ITimeContext
    {
        private readonly TimeLink _timeLink;
        private ushort _frameCycledRt;
        
        public ClientTimeContext(TimeLink timeLink)
        {
            _timeLink = timeLink;
            Update();
        }

        public void Update()
        {
            //Slog.Info($"{Time.frameCount}: {Time.deltaTime}");
            _frameCycledRt = _timeLink.RemoteTicker.CycledRt;
        }

        //TODO: use start of frame time point instead of instant value
        ushort ITimeContext.CurrentSessionCycledRt => _frameCycledRt;

        //TODO: use offset based on current smoothed server's send rate and rtt
        ushort ITimeContext.HistorySessionCycledRt => (ushort)(_frameCycledRt - 220 * Ticker.RtPerMs);
    }
}