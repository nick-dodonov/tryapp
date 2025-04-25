using System.Threading;
using System.Threading.Tasks;
using Locator.Api;
using Shared.Sys;
using Shared.Web;

namespace Locator.Client
{
    public class ClientLocator : ILocator
    {
        private readonly IWebClient _client;
        public ClientLocator(IWebClient client) => _client = client;

        public async ValueTask<StandInfo[]> GetStands(CancellationToken cancellationToken)
        {
            using var response = await _client.GetAsync("stands", cancellationToken);
            response.EnsureSuccessStatusCode();
            var content = await response.Content.SharedReadAsStringAsync(cancellationToken);
            var result = WebSerializer.Default.Deserialize<StandInfo[]>(content);
            return result;
        }
    }
}