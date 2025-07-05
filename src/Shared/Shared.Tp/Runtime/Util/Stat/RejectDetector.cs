using System.Runtime.CompilerServices;

namespace Shared.Tp.Util.Stat
{
    public struct RejectDetector
    {
        private const int RejectStartCount = 8;
        private const float RejectMinSigmas = 3.0f;
        private const int RejectMaxCount = 4;

        private int _rejectedCount;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Reset() => _rejectedCount = 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Check(int value, in CycleSampleSet set)
        {
            var stdDeviation = set.StdDeviation;
            if (set.Count >= RejectStartCount && stdDeviation > 0)
            {
                // Use rejection constraint to allow value "leaps", for example during
                //  * rtt estimation on network route changes 
                //  * send rate estimation on network or logical throttle because of bandwidth  
                // TODO: try adaptive sigma via exponential deviation or trend detection
                //  (possible faster adoption to new values)
                if (_rejectedCount < RejectMaxCount && set.FitSigmas(value, RejectMinSigmas))
                {
                    ++_rejectedCount;
                    //Slog.Info($"Rejected ({_rejectedCount}/{RejectMaxCount}) {value} by sigmas {sigmas:F1} > {RejectMinSigmas} (mean={_mean:F1} stdDev={_stdDeviation:F1})");
                    return false;
                }
            }

            return true;
        }
    }
}