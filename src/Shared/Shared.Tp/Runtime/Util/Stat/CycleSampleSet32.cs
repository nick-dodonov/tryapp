using System;
using System.Runtime.CompilerServices;

namespace Shared.Tp.Util.Stat
{
    public unsafe struct CycleSampleSet32 // TODO generate via T4 or SG
    {
        private const int MaxSize = 64;
        private fixed int _values[MaxSize];

        private int _currentIndex;
        private int _count;

        private long _valueSum; // for mean and deviation continuous calculations
        private long _valueSqrSum; // for standard deviation continuous calculation: `sum((x - mean)^2)` == `sum(x^2) - sum(x)^2 / n`

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(int value)
        {
            var oldValue = _values[_currentIndex];

            _valueSqrSum -= oldValue * oldValue;
            _valueSum -= oldValue;

            _values[_currentIndex] = value;
            _currentIndex = (_currentIndex + 1) % MaxSize;

            _valueSum += value;
            _valueSqrSum += value * value;

            if (++_count >= MaxSize)
                _count = MaxSize;
        }

        public float Mean
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if (_count > 0)
                    return (float)_valueSum / _count;
                return 0;
            }
        }

        public float StdDeviation
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                var besselCount = _count - 1; //https://en.wikipedia.org/wiki/Bessel%27s_correction
                if (besselCount > 0)
                {
                    var lessDiff = _valueSqrSum - _valueSum * _valueSum / _count;
                    if (lessDiff < 0.0f)
                        lessDiff = -lessDiff;
                    return MathF.Sqrt((float)lessDiff / besselCount);
                }
                return 0;
            }
        }
    }
}