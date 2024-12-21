using System;
using System.Net.Http;
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
            //ASP Web Controller default formatting
            _serializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web)
            {
                // IncludeFields = true
            };
        }
        
        public async ValueTask<ServerInfo> GetInfo(CancellationToken cancellationToken)
        {
            StaticLog.Info($"==== Info request: {_httpClient.BaseAddress}");
            using var response = await _httpClient.GetAsync("api/info", cancellationToken);
            response.EnsureSuccessStatusCode();
            StaticLog.Info($"==== Info response: StatusCode={response.StatusCode}");
            
            //TODO: PR to add System.Net.Http.Json to UnityNuGet (https://github.com/xoofx/UnityNuGet)
            //  to simplify usage instead of just System.Text.Json (adding support for encodings and mach more checks)
            //var result = await response.Content.ReadFromJsonAsync<string>(_serializerOptions, cancellationToken);
            await using var contentStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
            var result = await JsonSerializer.DeserializeAsync<ServerInfo>(contentStream, _serializerOptions, cancellationToken).ConfigureAwait(false);
            if (result == null)
                throw new Exception("deserialize failed");

            return result;
        }
    }
}