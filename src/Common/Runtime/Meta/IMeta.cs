using System;
using System.Threading;
using System.Threading.Tasks;
using Shared.Tp.Rtc;
using UnityEngine.Scripting;

namespace Common.Meta
{
    public interface IMeta : IDisposable
    {
        public IRtcService RtcService { get; }
        public ValueTask<ServerInfo> GetInfo(CancellationToken cancellationToken);
    }

    [Serializable]
    public class ServerInfo
    {
        // ReSharper disable UnusedMember.Global UnassignedField.Global NotAccessedField.Global
        [RequiredMember] public int RequestId;
        [RequiredMember] public DateTime RequestTime;
        [RequiredMember] public string? RandomName;
        // ReSharper restore UnusedMember.Global UnassignedField.Global NotAccessedField.Global
    }
}