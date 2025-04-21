using System;

namespace Shared.Boot.Version
{
    [Serializable]
    public struct BuildInfo
    {
        public string Provider;
        public string Sha;
        public string Branch;
        public DateTime Time;
    }
}