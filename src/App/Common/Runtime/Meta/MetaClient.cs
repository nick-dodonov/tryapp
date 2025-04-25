using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Shared.Log;
using Shared.Sys;
using Shared.Tp.Rtc;
using Shared.Web;

namespace Common.Meta
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

        public async ValueTask<MetaInfo> GetInfo(CancellationToken cancellationToken)
        {
            const string uri = "api/info";
            _logger.Info($"request: {_client.BaseAddress}{uri}");
            using var response = await _client.GetAsync(uri, cancellationToken);
            response.EnsureSuccessStatusCode();
            var content = await response.Content.SharedReadAsStringAsync(cancellationToken);
            _logger.Info($"response: {content}");
            var result = WebSerializer.Default.Deserialize<MetaInfo>(content);
            return result;
        }
    }
}
