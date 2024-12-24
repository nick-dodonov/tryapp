//TODO: test System.Text.Json with enabled WebGL embedded data (because System.Text.Json is better having span variants)
//WEBGL-DISABLE: using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Shared.Meta.Api;
using Shared.Web;

namespace Shared.Meta.Client
{
    public class MetaClient : IMeta
    {
        private readonly IWebClient _client;

        //ASP Web Controller default json formatting
        //WEBGL-DISABLE: private readonly JsonSerializerOptions _serializerOptions = new(JsonSerializerDefaults.Web);
        private static readonly JsonSerializerSettings JsonSettings = new()
        {
            ContractResolver = new Newtonsoft.Json.Serialization.DefaultContractResolver
            {
                NamingStrategy = new Newtonsoft.Json.Serialization.CamelCaseNamingStrategy()
            }
        };
        
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
            var uri = "api/info";
            StaticLog.Info($"GetInfo request: {_client.BaseAddress}{uri}");
            using var response = await _client.GetAsync(uri, cancellationToken);
            StaticLog.Info($"GetInfo response StatusCode: {response.StatusCode}");
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            StaticLog.Info($"GetInfo response Content: {content}");
            var result = JsonConvert.DeserializeObject<ServerInfo>(content, JsonSettings);

            //TODO: PR to add System.Net.Http.Json to UnityNuGet (https://github.com/xoofx/UnityNuGet)
            //  to simplify usage instead of just System.Text.Json (adding support for encodings and mach more checks)
            //var result = await response.Content.ReadFromJsonAsync<string>(_serializerOptions, cancellationToken);
            
            // //WebGL disabled: System.Text.Json.JsonSerializer doesn't work too
            // await using var contentStream = await response.Content.ReadAsStreamAsync(); //webgl-disabled: .ConfigureAwait(false);
            // var result = await JsonSerializer.DeserializeAsync<ServerInfo>(contentStream, _serializerOptions, cancellationToken); //webgl-disabled:.ConfigureAwait(false);
            
            if (result == null) throw new("deserialize failed");
            return result;
        }

        public async ValueTask<string> GetOffer(string id, CancellationToken cancellationToken)
        {
            var uri = $"api/getoffer?id={id}";
            StaticLog.Info($"==== GetOffer: {_client.BaseAddress}{uri}");
            using var response = await _client.GetAsync(uri, cancellationToken);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }
    }
}
