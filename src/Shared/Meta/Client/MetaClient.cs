using System.Threading;
using System.Threading.Tasks;
using Shared.Log;
using Shared.Meta.Api;
using Shared.Web;

namespace Shared.Meta.Client
{
    public class MetaClient : IMeta
    {
        private readonly IWebClient _client;

        public MetaClient(IWebClient client)
        {
            _client = client;
        }

        public void Dispose()
        {
            _client.Dispose();
        }

        public async ValueTask<ServerInfo> GetInfo(CancellationToken cancellationToken)
        {
            const string uri = "api/info";
            Slog.Info($"MetaClient: GetInfo: request: {_client.BaseAddress}{uri}");
            using var response = await _client.GetAsync(uri, cancellationToken);
            Slog.Info($"MetaClient: GetInfo: response StatusCode: {response.StatusCode}");
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            Slog.Info($"MetaClient: GetInfo: response Content: {content}");
            var result = WebSerializer.DeserializeObject<ServerInfo>(content);
            return result;

            //TODO: PR to add System.Net.Http.Json to UnityNuGet (https://github.com/xoofx/UnityNuGet)
            //  to simplify usage instead of just System.Text.Json (adding support for encodings and mach more checks)
            //var result = await response.Content.ReadFromJsonAsync<string>(_serializerOptions, cancellationToken);
            
            // //WebGL disabled: System.Text.Json.JsonSerializer doesn't work too
            // await using var contentStream = await response.Content.ReadAsStreamAsync(); //webgl-disabled: .ConfigureAwait(false);
            // var result = await JsonSerializer.DeserializeAsync<ServerInfo>(contentStream, _serializerOptions, cancellationToken); //webgl-disabled:.ConfigureAwait(false);
        }

        public async ValueTask<string> GetOffer(string id, CancellationToken cancellationToken)
        {
            var uri = $"api/getoffer?id={id}";
            Slog.Info($"MetaClient: GetOffer: {_client.BaseAddress}{uri}");
            using var response = await _client.GetAsync(uri, cancellationToken);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }

        public async ValueTask<string> SetAnswer(string id, string answer, CancellationToken cancellationToken)
        {
            var uri = $"api/setanswer?id={id}";
            Slog.Info($"MetaClient: SetAnswer: {_client.BaseAddress}{uri}");
            using var response = await _client.PostAsync(uri, answer, cancellationToken);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }

        public async ValueTask AddIceCandidates(string id, string candidates, CancellationToken cancellationToken)
        {
            var uri = $"api/addicecandidates?id={id}";
            Slog.Info($"MetaClient: AddIceCandidates: {_client.BaseAddress}{uri}");
            using var response = await _client.PostAsync(uri, candidates, cancellationToken);
            response.EnsureSuccessStatusCode();
        }
    }
}
