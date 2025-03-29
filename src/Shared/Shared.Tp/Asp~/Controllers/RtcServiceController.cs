using Microsoft.AspNetCore.Mvc;
using Shared.Tp.Rtc;

namespace Shared.Controllers;

[ApiController]
[Route("api/[action]")]
public sealed class RtcServiceController(IRtcService rtcService) : ControllerBase, IRtcService
{
    [HttpGet]
    public ValueTask<RtcOffer> GetOffer(CancellationToken cancellationToken) =>
        rtcService.GetOffer(cancellationToken);

    [HttpPost]
    public ValueTask<RtcIcInit[]> SetAnswer(
        string token, [FromBody] RtcSdpInit answer, CancellationToken cancellationToken) =>
        rtcService.SetAnswer(token, answer, cancellationToken);

    [HttpPost]
    public ValueTask AddIceCandidates(
        string token, [FromBody] RtcIcInit[] candidates, CancellationToken cancellationToken) =>
        rtcService.AddIceCandidates(token, candidates, cancellationToken);
}