using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Shared.Sys
{
    public interface ISharedSystem
    {
        public Task<string> HttpContent_ReadAsStringAsync(HttpContent content, CancellationToken cancellationToken);
    }
}