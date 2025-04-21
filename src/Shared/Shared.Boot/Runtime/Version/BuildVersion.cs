using System;

namespace Shared.Boot.Version
{
    [Serializable]
    public struct BuildVersion
    {
        public string Sha;
        public string Branch;
        public DateTime Time;
    }
}