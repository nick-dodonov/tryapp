using Locator.Api;
using Microsoft.AspNetCore.Mvc;

namespace Locator.Service;

[ApiController]
[Route("")]
public class LocatorController : ILocator
{
    [Route("stands2")]
    public ValueTask<StandInfo[]> GetStands(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}