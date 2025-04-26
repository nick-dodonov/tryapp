using System;

namespace Shared.Tp.Ext.Misc
{
    [Serializable]
    public class DumpStats
    {
        [Serializable]
        public struct Dir
        {
            public int Count;
            public int Bytes;
            public int Rate;

            private long _startTicks;
            private int _bytes;
                
            public void Add(int bytes)
            {
                _bytes += bytes;
                Bytes += bytes;
                Count++;
            }

            public void UpdateRate(long ticks, int updateIntervalMs)
            {
                if (_startTicks == 0)
                {
                    _startTicks = ticks;
                    _bytes = 0;
                }
                else
                {
                    var updateIntervalTicks = TimeSpan.TicksPerMillisecond * updateIntervalMs;
                    var delta = ticks - _startTicks;
                    if (delta >= updateIntervalTicks)
                    {
                        Rate = (int)(updateIntervalTicks * _bytes / delta);
                        _bytes = 0;
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