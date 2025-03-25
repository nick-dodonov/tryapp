using System;
using System.Collections.Generic;
using System.Linq;
using Shared.Web;
using UnityEngine;
using UnityEngine.Scripting;

namespace Shared.Tp.Rtc
{
    [Serializable]
    public class RtcConfig
    {
        [field: SerializeField] [RequiredMember]
        public List<RtcIceServer>? IceServers { get; set; }

        public override string ToString() =>
            $"{nameof(RtcConfig)}({string.Join(", ", IceServers?.Select(x => $"\"{x.Url}\"") ?? Enumerable.Empty<string>())})";

        private string? _json;
        public string ToJson() => _json ??= WebSerializer.Default.Serialize(this);
    }

    [Serializable]
    public class RtcIceServer
    {
        [field: SerializeField] [RequiredMember]
        public string? Url { get; set; }

        [field: SerializeField] [RequiredMember]
        public string? Username { get; set; }

        [field: SerializeField] [RequiredMember]
        public string? Password { get; set; }
    }
}