using System;

namespace Shared.Tp.St.Sync
{
    public readonly struct StKey : IComparable<StKey>
    {
        public readonly int Frame;
        public readonly ushort CycledRt;

        private StKey(int frame, ushort cycledRt) => (Frame, CycledRt) = (frame, cycledRt);

        public int CompareTo(StKey other)
        {
            //var otherFrame = other.Frame;
            // if (Frame != 0 && otherFrame != 0)
            //     return Frame.CompareTo(otherFrame);
            // return Ms.CompareTo(other.Ms);
            var otherCycledRt = other.CycledRt;
            if (CycledRt != 0 && otherCycledRt != 0)
                return CycledRt.CompareTo(otherCycledRt);
            return Frame.CompareTo(other.Frame);
        }

        //public void Deconstruct(out int frame, out int ms) => (frame, ms) = (Frame, Ms);

        public static implicit operator StKey((int frame, ushort cycledRt) value) =>
            new(value.frame, value.cycledRt);

        // public static implicit operator (int frame, int ms)(StKey value) => 
        //     (value.Frame, value.Ms);
    }
}