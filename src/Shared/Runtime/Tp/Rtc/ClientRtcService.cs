using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Shared.Log;
using Shared.Web;

namespace Shared.Tp.Rtc
{
    public class ClientRtcService : IRtcService
    {
        private readonly IWebClient _client;
        private readonly ILogger _logger;

        public ClientRtcService(IWebClient client, ILogger<ClientRtcService> logger)
        {
            _client = client;
            _logger = logger;
        }
        
        private static string GetUri(string? query = null, [CallerMemberName] string action = "") =>
            query != null 
                ? $"api/{action}?{query}" 
                : $"api/{action}";

        public async ValueTask<RtcOffer> GetOffer(CancellationToken cancellationToken)
        {
            var uri = GetUri();
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
            var uri = GetUri($"token={token}");
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
            var uri = GetUri($"token={token}");
            _logger.Info($"POST: {_client.BaseAddress}{uri}");

            var json = WebSerializer.SerializeObject(candidates);
            using var response = await _client.PostAsync(uri, json, cancellationToken);
            response.EnsureSuccessStatusCode();
            _logger.Info("OK");
        }
    }
}