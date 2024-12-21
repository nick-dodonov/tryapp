using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Shared.Meta.Api;

namespace Shared.Meta.Client
{
    public class MetaClient : IMeta
    {
        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _serializerOptions;
        
        public MetaClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
            //TODO: setup ASP Web Controller default formatting
            _serializerOptions = new JsonSerializerOptions
            {
                // PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                // IncludeFields = true
            };
        }
        
        public async ValueTask<string> GetDateTime(CancellationToken cancellationToken)
        {
            using var response = await _httpClient.GetAsync("api/datetime", cancellationToken);
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<string>(_serializerOptions, cancellationToken);
            return result!;
        }
    }
}