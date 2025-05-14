using System;

namespace Shared.Tp.St.Sync
{
    public readonly struct StKey : IComparable<StKey>
    {
        public readonly int Frame;
        public readonly int Ms;

        private StKey(int frame, int ms) => (Frame, Ms) = (frame, ms);

        public int CompareTo(StKey other)
        {
            var otherFrame = other.Frame;
            if (Frame != 0 && otherFrame != 0)
                return Frame.CompareTo(otherFrame);
            return Ms.CompareTo(other.Ms);
        }

        //public void Deconstruct(out int frame, out int ms) => (frame, ms) = (Frame, Ms);

        public static implicit operator StKey((int frame, int ms) value) =>
            new(value.frame, value.ms);

        // public static implicit operator (int frame, int ms)(StKey value) => 
        //     (value.Frame, value.Ms);
    }
}