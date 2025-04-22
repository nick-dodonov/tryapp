using System;
using System.Threading;
using System.Threading.Tasks;
using Shared.Boot.Version;
using Shared.Tp.Rtc;
using UnityEngine.Scripting;

namespace Common.Meta
{
    public interface IMeta : IDisposable
    {
        public IRtcService RtcService { get; }
        public ValueTask<MetaInfo> GetInfo(CancellationToken cancellationToken);
    }

    [Serializable]
    public class MetaInfo
    {
        // ReSharper disable UnusedMember.Global UnassignedField.Global NotAccessedField.Global
        [RequiredMember] public int RequestId;
        [RequiredMember] public DateTime RequestTime;
        [RequiredMember] public BuildVersion BuildVersion;
        // ReSharper restore UnusedMember.Global UnassignedField.Global NotAccessedField.Global
    }
}