using System;

namespace Shared.Tp.Ext.Misc
{
    [Serializable]
    public class DumpStats
    {
        [Serializable]
        public struct Dir
        {
            public int CountTotal;
            public int BytesTotal;

            public int CountRate;
            public int BytesRate;

            private long _startTicks;
            private int _intervalCount;
            private int _intervalBytes;
                
            public void Add(int bytes)
            {
                BytesTotal += bytes;
                ++CountTotal;

                _intervalBytes += bytes;
                ++_intervalCount;
            }

            public void UpdateRate(long ticks, int updateIntervalMs)
            {
                if (_startTicks == 0)
                {
                    _startTicks = ticks;
                    _intervalCount = _intervalBytes = 0;
                }
                else
                {
                    var updateIntervalTicks = TimeSpan.TicksPerMillisecond * updateIntervalMs;
                    var delta = ticks - _startTicks;
                    if (delta >= updateIntervalTicks)
                    {
                        CountRate = _intervalCount;
                        BytesRate = _intervalBytes; //(int)(updateIntervalTicks * _intervalBytes / delta);
                        _intervalCount = _intervalBytes = 0;
                        _startTicks = ticks;
                    }
                }
            }
        }

        public Dir In;
        public Dir Out;

        //TODO: think to update on every Add 
        public DumpStats UpdateRates(int updateIntervalMs = 1000)
        {
            var ticks = DateTime.UtcNow.Ticks;
            In.UpdateRate(ticks, updateIntervalMs);
            Out.UpdateRate(ticks, updateIntervalMs);
            return this;
        }
    }
}