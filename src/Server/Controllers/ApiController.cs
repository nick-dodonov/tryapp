using Microsoft.AspNetCore.Mvc;
using Shared.Meta.Api;
using Shared.Tp.Rtc;

namespace server.Controllers;

[ApiController]
[Route("[controller]")]
public sealed class ApiController(IMeta meta) 
    : ControllerBase
{
    public void Dispose() { }

    [Route("info")]
    public ValueTask<ServerInfo> GetInfo(CancellationToken cancellationToken) 
        => meta.GetInfo(cancellationToken);

    [Route("getoffer")]
    public ValueTask<RtcOffer> GetOffer(CancellationToken cancellationToken)
        => meta.GetOffer(cancellationToken);

    [HttpPost]
    [Route("setanswer")]
    // public ValueTask<string> SetAnswer(string token, [FromBody] RTCSessionDescriptionInit answer, CancellationToken cancellationToken)
    public async ValueTask<string> SetAnswer(string token, CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(HttpContext.Request.Body);
        var answerJson = await reader.ReadToEndAsync(cancellationToken);
        return await meta.SetAnswer(token, answerJson, cancellationToken);
    }
    
    [HttpPost]
    [Route("addicecandidates")]
    public async ValueTask AddIceCandidates(string token, CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(HttpContext.Request.Body);
        var candidatesJson = await reader.ReadToEndAsync(cancellationToken);
        await meta.AddIceCandidates(token, candidatesJson, cancellationToken);
    }
}