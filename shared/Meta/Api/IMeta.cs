using System;
using System.Threading;
using System.Threading.Tasks;

namespace Shared.Meta.Api
{
    public interface IMeta : IDisposable
    {
        public ValueTask<ServerInfo> GetInfo(CancellationToken cancellationToken);

        //TODO: separate iface
        //TODO: shared RTCSessionDescription
        public ValueTask<string> GetOffer(string id, CancellationToken cancellationToken);
    }
    
    [Serializable]
    public class ServerInfo
    {
        public string? RandomName;
        public int RequestId;
        public DateTime RequestTime;
    }
}