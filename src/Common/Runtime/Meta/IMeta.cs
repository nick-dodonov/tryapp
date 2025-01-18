using System;
using System.Threading;
using System.Threading.Tasks;
using Shared.Tp.Rtc;

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
        // ReSharper disable UnassignedField.Global NotAccessedField.Global
        public int RequestId;
        public DateTime RequestTime;
        public string? RandomName;
        // ReSharper restore UnassignedField.Global NotAccessedField.Global
    }
}