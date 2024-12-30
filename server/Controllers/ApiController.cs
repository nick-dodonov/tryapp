using Microsoft.AspNetCore.Mvc;
using Shared.Meta.Api;

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
    public ValueTask<string> GetOffer(string id, CancellationToken cancellationToken)
        => meta.GetOffer(id, cancellationToken);

    [HttpPost]
    [Route("setanswer")]
    public async ValueTask<string> SetAnswer(string id, CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(HttpContext.Request.Body);
        var answerJson = await reader.ReadToEndAsync(cancellationToken);
        return await meta.SetAnswer(id, answerJson, cancellationToken);
    }
    // [HttpPost]
    // [Route("setanswer-TODO")]
    // public ValueTask<string> SetAnswer(string id, [FromBody] RTCSessionDescriptionInit answer, CancellationToken cancellationToken) 
    //     => rtcService.SetAnswer(id, answer, cancellationToken);
    
    [HttpPost]
    [Route("addicecandidates")]
    public async ValueTask AddIceCandidates(string id, CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(HttpContext.Request.Body);
        var candidatesJson = await reader.ReadToEndAsync(cancellationToken);
        await meta.AddIceCandidates(id, candidatesJson, cancellationToken);
    }
}