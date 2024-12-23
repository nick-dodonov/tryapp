using Microsoft.AspNetCore.Mvc;
using Server.Rtc;
using Shared.Meta.Api;
using SIPSorcery.Net;

namespace server.Controllers;

[ApiController]
[Route("[controller]")]
public class ApiController(
    ILogger<ApiController> logger, 
    IMeta meta, 
    RtcService rtcService) 
    : ControllerBase, IMeta
{
    [Route("info")]
    public ValueTask<ServerInfo> GetInfo(CancellationToken cancellationToken) 
        => meta.GetInfo(cancellationToken);
    
    [Route("getoffer")]
    public async Task<IActionResult> GetOffer(string id)
    {
        logger.LogDebug($"GetOffer: {id}");
        return Ok(await rtcService.GetOffer(id));
    }
    
    [HttpPost]
    [Route("setanswer")]
    public IActionResult SetAnswer(string id, [FromBody] RTCSessionDescriptionInit answer)
    {
        logger.LogDebug($"SetAnswer: {id} {answer.type} {answer.sdp}");

        if (string.IsNullOrWhiteSpace(id))
            return BadRequest("The id cannot be empty in SetAnswer");
        if (string.IsNullOrWhiteSpace(answer.sdp))
            return BadRequest("The SDP cannot be empty in SetAnswer");

        rtcService.SetRemoteDescription(id, answer);
        return Ok();
    }
    
    [Route("testsend")]
    public void TestSend(string id)
    {
        logger.LogDebug($"TestSend: {id}");
        rtcService.TestSend(id);
    }
}