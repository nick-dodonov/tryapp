using System;
using System.Runtime.CompilerServices;

namespace Shared.Tp.Util.Stat
{
    public unsafe struct CycleSampleSet
    {
        private const int MaxSize = 48; // TODO generate different via T4 or SG
        private fixed int _values[MaxSize];

        private int _currentIndex;
        private int _count;

        private long _sum; // for mean and deviation continuous calculations
        private long _sqrSum; // for standard deviation continuous calculation: `sum((x - mean)^2)` == `sum(x^2) - sum(x)^2 / n`

        private int _meanInt;
        private float _mean;
        private float _stdDeviation;

        public struct ResultData
        {
            public int Count;
            public float Mean;
            public float StdDeviation;
        }

        public ResultData Result => new()
        {
            Count = _count,
            Mean = _mean,
            StdDeviation = _stdDeviation,
        };
        
        public int Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _count;
        }

        public int MeanInt
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _meanInt;
        }

        public float Mean
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _mean;
        }

        public float StdDeviation
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _stdDeviation;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(int value)
        {
            var oldValue = _values[_currentIndex];

            _sqrSum -= oldValue * oldValue;
            _sum -= oldValue;

            _values[_currentIndex] = value;
            _currentIndex = (_currentIndex + 1) % MaxSize;

            _sum += value;
            _sqrSum += value * value;

            if (++_count >= MaxSize)
                _count = MaxSize;

            _meanInt = (int)(_sum / _count);
            _mean = (float)_sum / _count;
            var besselCount = _count - 1; //https://en.wikipedia.org/wiki/Bessel%27s_correction
            if (besselCount > 0)
            {
                var lessDiff = _sqrSum - _sum * _sum / _count;
                if (lessDiff < 0.0f)
                    lessDiff = -lessDiff;
                _stdDeviation = MathF.Sqrt((float)lessDiff / besselCount);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool FitSigmas(int value, float minSigmas)
        {
            if (_stdDeviation <= 0)
                return false;

            var deviation = value - _mean;
            var sigmas = deviation / _stdDeviation;

            return -minSigmas <= sigmas && sigmas <= minSigmas;
        }
    }
}