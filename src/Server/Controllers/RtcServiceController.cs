using Microsoft.AspNetCore.Mvc;
using Shared.Tp.Rtc;

namespace server.Controllers;

[ApiController]
[Route("api")]
public sealed class RtcServiceController(IRtcService rtcService) : ControllerBase, IRtcService
{
    [HttpGet("getoffer")]
    public ValueTask<RtcOffer> GetOffer(CancellationToken cancellationToken) =>
        rtcService.GetOffer(cancellationToken);

    [HttpPost("setanswer")]
    public ValueTask<RtcIcInit[]> SetAnswer(
        string token, [FromBody] RtcSdpInit answer, CancellationToken cancellationToken) =>
        rtcService.SetAnswer(token, answer, cancellationToken);

    [HttpPost("addicecandidates")]
    public ValueTask AddIceCandidates(
        string token, [FromBody] RtcIcInit[] candidates, CancellationToken cancellationToken) =>
        rtcService.AddIceCandidates(token, candidates, cancellationToken);
}