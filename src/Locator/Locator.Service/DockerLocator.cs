using Locator.Api;

namespace Locator.Service;

public class DockerLocator : ILocator
{
    public ValueTask<StandInfo[]> GetStands(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}