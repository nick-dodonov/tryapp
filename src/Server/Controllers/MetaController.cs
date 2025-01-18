using Microsoft.AspNetCore.Mvc;
using Shared.Meta.Api;
using Shared.Tp.Rtc;

namespace server.Controllers;

[ApiController]
[Route("api")] //"[controller]"
public sealed class MetaController(IMeta meta, IRtcService rtcService) 
    : ControllerBase, IMeta
{
    public void Dispose() { }

    IRtcService IMeta.RtcService => rtcService;

    [Route("info")]
    public ValueTask<ServerInfo> GetInfo(CancellationToken cancellationToken) =>
        meta.GetInfo(cancellationToken);
}