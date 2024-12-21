using System.Threading;
using System.Threading.Tasks;

namespace Shared.Meta.Api
{
    public interface IMeta
    {
        public ValueTask<string> GetDateTime(CancellationToken cancellationToken); //just sample
    }
}