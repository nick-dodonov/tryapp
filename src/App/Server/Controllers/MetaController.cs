using Common.Meta;
using Microsoft.AspNetCore.Mvc;
using Shared.Tp.Rtc;

namespace Server.Controllers;

[ApiController]
[Route("api")] //"[controller]"
public sealed class MetaController(IMeta meta, IRtcService rtcService) 
    : ControllerBase, IMeta
{
    public void Dispose() { }

    IRtcService IMeta.RtcService => rtcService;

    [Route("info")]
    public ValueTask<MetaInfo> GetInfo(CancellationToken cancellationToken)
        => meta.GetInfo(cancellationToken);
}