using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Shared.Log;
using Shared.Meta.Api;
using Shared.Tp.Rtc;
using Shared.Web;

namespace Shared.Meta.Client
{
    public class MetaClient : IMeta
    {
        private readonly ILogger _logger;
        
        private readonly IWebClient _client;
        private readonly IRtcService _rtcService;

        public MetaClient(IWebClient client, ILoggerFactory loggerFactory)
        {
            _client = client;
            _logger = loggerFactory.CreateLogger<MetaClient>();
            _rtcService = new ClientRtcService(client, loggerFactory.CreateLogger<ClientRtcService>());
        }

        public void Dispose() { }

        IRtcService IMeta.RtcService => _rtcService;

        public async ValueTask<ServerInfo> GetInfo(CancellationToken cancellationToken)
        {
            const string uri = "api/info";
            _logger.Info($"request: {_client.BaseAddress}{uri}");
            using var response = await _client.GetAsync(uri, cancellationToken);
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            _logger.Info($"response: {content}");
            var result = WebSerializer.DeserializeObject<ServerInfo>(content);
            return result;

            //TODO: PR to add System.Net.Http.Json to UnityNuGet (https://github.com/xoofx/UnityNuGet)
            //  to simplify usage instead of just System.Text.Json (adding support for encodings and mach more checks)
            //var result = await response.Content.ReadFromJsonAsync<string>(_serializerOptions, cancellationToken);
            
            // //WebGL disabled: System.Text.Json.JsonSerializer doesn't work too
            // await using var contentStream = await response.Content.ReadAsStreamAsync(); //webgl-disabled: .ConfigureAwait(false);
            // var result = await JsonSerializer.DeserializeAsync<ServerInfo>(contentStream, _serializerOptions, cancellationToken); //webgl-disabled:.ConfigureAwait(false);
        }
    }
}
