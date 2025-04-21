using System;

namespace Shared.Boot.Version
{
    public struct BuildInfo
    {
        public string Provider;
        public string Sha;
        public string Branch;
        public DateTime Time;
    }
}