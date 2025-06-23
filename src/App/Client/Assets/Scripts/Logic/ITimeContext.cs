using Shared.Tp.Chrono;
using Shared.Tp.Ext.Misc;

namespace Client.Logic
{
    public interface ITimeContext
    {
        public Tick<long, RawPeriod> NowTick { get; }
        public Tick<long, RawPeriod> HistoryTick { get; }
    }
}