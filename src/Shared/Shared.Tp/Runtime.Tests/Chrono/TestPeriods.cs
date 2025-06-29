using Shared.Tp.Chrono;

namespace Shared.Tp.Tests.Chrono
{
    internal readonly struct TestPeriod: IPeriod
    {
        public const long CountPerSec = 1_000_000;
        public long InstanceCountPerSec => CountPerSec;
    }

    internal readonly struct SecPeriod: IPeriod
    {
        public const long CountPerSec = 1;
        public long InstanceCountPerSec => CountPerSec;
    }
}