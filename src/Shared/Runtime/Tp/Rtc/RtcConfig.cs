using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Scripting;

namespace Shared.Tp.Rtc
{
    [Serializable]
    public class RtcConfig
    {
        public List<RtcIceServer>? IceServers { get; set; }

        public override string ToString() =>
            $"{nameof(RtcConfig)}({string.Join(", ", IceServers?.Select(x => $"\"{x.Url}\"") ?? Enumerable.Empty<string>())})";
    }

    [Serializable]
    public class RtcIceServer
    {
        //[field: SerializeField] [RequiredMember]
        public string? Url { get; set; }

        //[field: SerializeField] [RequiredMember]
        public string? Username { get; set; }

        //[field: SerializeField] [RequiredMember]
        public string? Password { get; set; }
    }
}