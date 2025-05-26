using System;
using System.Runtime.CompilerServices;
using Shared.Log;

namespace Shared.Tp.Util.Stat
{
    public unsafe struct CycleSampleSet32 // TODO generate via T4 or SG
    {
        private const int MinSizeToReject = 8;
        private const int MaxSize = 64;
        private fixed int _values[MaxSize];

        private int _currentIndex;
        private int _count;

        private long _sum; // for mean and deviation continuous calculations
        private long _sqrSum; // for standard deviation continuous calculation: `sum((x - mean)^2)` == `sum(x^2) - sum(x)^2 / n`

        private float _mean;
        private float _stdDeviation;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(int value)
        {
            if (_count >= MinSizeToReject && _stdDeviation > 0)
            {
                var deviation = MathF.Abs(value - _mean);
                var sigmas = deviation / _stdDeviation;
                
                //TODO: make adaptive allowing values leap (via exponential deviation or via trend detection or via reject count)
                const float adaptiveThreshold = 3.0f;
                if (sigmas > adaptiveThreshold)
                {
                    //TODO: rm log spamming
                    Slog.Info($"Reject {value} by sigmas={sigmas:F1} mean={_mean:F1} stdDev={_stdDeviation:F1}");
                    return;
                }
            }

            var oldValue = _values[_currentIndex];

            _sqrSum -= oldValue * oldValue;
            _sum -= oldValue;

            _values[_currentIndex] = value;
            _currentIndex = (_currentIndex + 1) % MaxSize;

            _sum += value;
            _sqrSum += value * value;

            if (++_count >= MaxSize)
                _count = MaxSize;
            
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
    }
}