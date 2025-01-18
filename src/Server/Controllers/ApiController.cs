using Microsoft.AspNetCore.Mvc;
using Shared.Meta.Api;
using Shared.Tp.Rtc;

namespace server.Controllers;

[ApiController]
[Route("[controller]")]
public sealed class ApiController(IMeta meta) 
    : ControllerBase
    , IRtcService
{
    public void Dispose() { }

    [Route("info")]
    public ValueTask<ServerInfo> GetInfo(CancellationToken cancellationToken) => 
        meta.GetInfo(cancellationToken);

    [Route("getoffer")]
    public ValueTask<RtcOffer> GetOffer(CancellationToken cancellationToken) => 
        meta.GetOffer(cancellationToken);

    [HttpPost]
    [Route("setanswer")]
    public ValueTask<RtcIcInit[]> SetAnswer(string token, [FromBody] RtcSdpInit answer, CancellationToken cancellationToken) =>
        // using var reader = new StreamReader(HttpContext.Request.Body);
        // var json = await reader.ReadToEndAsync(cancellationToken);
        // var answer = WebSerializer.DeserializeObject<RtcSdpInit>(json);
        meta.SetAnswer(token, answer, cancellationToken);

    [HttpPost]
    [Route("addicecandidates")]
    public ValueTask AddIceCandidates(string token, [FromBody] RtcIcInit[] candidates, CancellationToken cancellationToken) =>
        meta.AddIceCandidates(token, candidates, cancellationToken);
}