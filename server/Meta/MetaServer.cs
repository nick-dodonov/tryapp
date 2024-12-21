using Shared;
using Shared.Meta.Api;

namespace Server.Meta;

public class MetaServer : IMeta
{
    public ValueTask<ServerInfo> GetInfo(CancellationToken cancellationToken)
    {
        var result = new ServerInfo
        {
            RequestTime = DateTime.Now
        };
        StaticLog.Info($"==== Info request/result: {result.RequestTime}");
        return new ValueTask<ServerInfo>(result);
    }
}