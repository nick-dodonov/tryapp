using System.Threading;
using System.Threading.Tasks;
using Locator.Api;

namespace Locator.Client
{
    public class ClientLocator : ILocator
    {
        public ValueTask<StandInfo[]> GetStands(CancellationToken cancellationToken)
        {
            throw new System.NotImplementedException();
        }
    }
}