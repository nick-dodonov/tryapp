using Common.Meta;
using Shared.Boot.Asp.Version;
using Shared.Log;
using Shared.Tp.Rtc;
using Shared.Web;

namespace Server.Meta;

public sealed class MetaServer(
    IRtcService rtcService, 
    ILogger<MetaServer> logger) 
    : IMeta
{
    private int _uid;

    public void Dispose() { }

    IRtcService IMeta.RtcService => rtcService;

    ValueTask<MetaInfo> IMeta.GetInfo(CancellationToken cancellationToken)
    {
        var result = new MetaInfo
        {
            RequestId = ++_uid,
            RequestTime = DateTime.Now,
            BuildVersion = AspVersionProvider.BuildVersion
        };

        logger.Info(WebSerializer.Default.Serialize(result));
        return new(result);
    }
}