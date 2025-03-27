using Locator.Api;
using Microsoft.AspNetCore.Mvc;

namespace Locator.Service;

[ApiController]
[Route("")]
public class LocatorController(ILocator impl) : ILocator
{
    [Route("stands")]
    public ValueTask<StandInfo[]> GetStands(CancellationToken cancellationToken) =>
        impl.GetStands(cancellationToken);
}