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
        private readonly IWebClient _client;
        private readonly ILogger<MetaClient> _logger;

        public MetaClient(IWebClient client, ILogger<MetaClient> logger)
        {
            _client = client;
            _logger = logger;
        }

        public void Dispose()
        {
            _client.Dispose();
        }

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

        public async ValueTask<RtcOffer> GetOffer(CancellationToken cancellationToken)
        {
            var uri = "api/getoffer";
            _logger.Info($"GET: {_client.BaseAddress}{uri}");

            using var response = await _client.GetAsync(uri, cancellationToken);
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            _logger.Info($"RET: {content}");

            var result = WebSerializer.DeserializeObject<RtcOffer>(content);
            return result;
        }

        public async ValueTask<RtcIcInit[]> SetAnswer(string token, RtcSdpInit answer, CancellationToken cancellationToken)
        {
            var uri = $"api/setanswer?token={token}";
            _logger.Info($"POST: {_client.BaseAddress}{uri}");

            var json = WebSerializer.SerializeObject(answer);
            using var response = await _client.PostAsync(uri, json, cancellationToken);
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            _logger.Info($"RET: {content}");

            var result = WebSerializer.DeserializeObject<RtcIcInit[]>(content);
            return result;
        }

        public async ValueTask AddIceCandidates(string token, RtcIcInit[] candidates, CancellationToken cancellationToken)
        {
            var uri = $"api/addicecandidates?token={token}";
            _logger.Info($"POST: {_client.BaseAddress}{uri}");

            var json = WebSerializer.SerializeObject(candidates);
            using var response = await _client.PostAsync(uri, json, cancellationToken);
            response.EnsureSuccessStatusCode();
            _logger.Info("OK");
        }
    }
}
