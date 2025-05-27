using System;
using System.Runtime.CompilerServices;

namespace Shared.Tp.Util.Stat
{
    public unsafe struct CycleSampleSet // TODO generate via T4 or SG
    {
        private const int MaxSize = 48;
        private const int RejectStartCount = 8;
        private const float RejectMinSigmas = 3.0f;
        private const int RejectMaxCount = 4;

        private fixed int _values[MaxSize];

        private int _currentIndex;
        private int _count;

        private long _sum; // for mean and deviation continuous calculations
        private long _sqrSum; // for standard deviation continuous calculation: `sum((x - mean)^2)` == `sum(x^2) - sum(x)^2 / n`

        private int _meanInt;
        private float _mean;
        private float _stdDeviation;

        private int _rejectedCount;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(int value)
        {
            if (_count >= RejectStartCount && _stdDeviation > 0)
            {
                var deviation = MathF.Abs(value - _mean);
                var sigmas = deviation / _stdDeviation;

                // Use rejection constraint to allow value "leaps", for example during
                //  * rtt estimation on network route changes 
                //  * send rate estimation on network or logical throttle because of bandwidth  
                // TODO: try adaptive sigma via exponential deviation or trend detection
                //  (possible faster adoption to new values)
                if (sigmas > RejectMinSigmas && _rejectedCount < RejectMaxCount)
                {
                    ++_rejectedCount;
                    //Slog.Info($"Rejected ({_rejectedCount}/{RejectMaxCount}) {value} by sigmas {sigmas:F1} > {RejectMinSigmas} (mean={_mean:F1} stdDev={_stdDeviation:F1})");
                    return;
                }
            }

            _rejectedCount = 0;

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
    }

    internal static class MathExtensions
    {
        public static float Lerp(float a, float b, float t)
        {
            t = t < 0f ? 0f : (t > 1f ? 1f : t);
            return a + (b - a) * t;
        }
    }
}