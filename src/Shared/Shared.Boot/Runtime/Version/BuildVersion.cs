using System;

namespace Shared.Boot.Version
{
    [Serializable]
    public struct BuildVersion
    {
        public string Ref;
        public string Sha;
        public DateTime Time;
    }
}