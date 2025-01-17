using System;
using System.Threading;
using System.Threading.Tasks;
using Shared.Tp.Rtc;

namespace Shared.Meta.Api
{
    public interface IMeta : IRtcService, IDisposable
    {
        public ValueTask<ServerInfo> GetInfo(CancellationToken cancellationToken);
    }
    
    [Serializable]
    public class ServerInfo
    {
        public string? RandomName;
        public int RequestId;
        public DateTime RequestTime;
    }
}