using Shared;
using Shared.Meta.Api;

namespace Server.Meta;

public class MetaServer : IMeta
{
    public ValueTask<string> GetDateTime(CancellationToken cancellationToken)
    {
        var result = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        StaticLog.Info($"==== Now request/result: {result}");
        return new ValueTask<string>(result);
    }
}