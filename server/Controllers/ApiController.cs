using Microsoft.AspNetCore.Mvc;
using Server.Rtc;
using Shared.Meta.Api;
using SIPSorcery.Net;

namespace server.Controllers;

[ApiController]
[Route("[controller]")]
public sealed class ApiController(
    ILogger<ApiController> logger, 
    IMeta meta, 
    RtcService rtcService) 
    : ControllerBase, IMeta
{
    public void Dispose() => logger.LogDebug("Dispose"); // diagnose controller behaviour

    [Route("info")]
    public ValueTask<ServerInfo> GetInfo(CancellationToken cancellationToken) 
        => meta.GetInfo(cancellationToken);
    
    [Route("getoffer")]
    public ValueTask<string> GetOffer(string id, CancellationToken cancellationToken)
        => meta.GetOffer(id, cancellationToken);
    
    [HttpPost]
    [Route("setanswer")]
    public IActionResult SetAnswer(string id, [FromBody] RTCSessionDescriptionInit answer)
    {
        if (string.IsNullOrWhiteSpace(id))
            return BadRequest("The id cannot be empty in SetAnswer");
        if (string.IsNullOrWhiteSpace(answer.sdp))
            return BadRequest("The SDP cannot be empty in SetAnswer");

        rtcService.SetRemoteDescription(id, answer);
        return Ok();
    }
    
    [Route("testsend")]
    public void TestSend(string id) 
        => rtcService.TestSend(id);
}