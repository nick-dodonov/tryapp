using System;
using System.Collections.Generic;

namespace Shared.Tp.Rtc
{
    [Serializable]
    public class RtcConfig
    {
        public List<RtcIceServer>? IceServers { get; set; }
    }

    [Serializable]
    public class RtcIceServer
    {
        public string? Url { get; set; }
        public string? Username { get; set; }
        public string? Password { get; set; }
    }
}