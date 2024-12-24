using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace Shared.Web
{
    /// <summary>
    /// Wrapper workaround the issue standard HttpClient doesn't work in Unity WebGL
    /// TODO: also WebGL Threading Patcher is also useful
    ///     https://github.com/VolodymyrBS/WebGLThreadingPatcher
    /// </summary>
    public interface IWebClient : IDisposable
    {
        Uri BaseAddress { get; set; }
        HttpRequestHeaders DefaultRequestHeaders { get; }

        Task<HttpResponseMessage> SendAsync(HttpRequestMessage message, HttpCompletionOption option, CancellationToken token);
        Task<HttpResponseMessage> PostAsync(string uri, string answer, CancellationToken cancellationToken);
    }
    
    public interface IWebClientFactory
    {
        IWebClient CreateClient();
    }
    
    public static class WebClientExtensions
    {
        public static Task<string> GetStringAsync(this IWebClient client, string? requestUri, CancellationToken cancellationToken) =>
            client.GetStringAsync(CreateUri(requestUri), cancellationToken);

        public static async Task<string> GetStringAsync(this IWebClient client, Uri? requestUri, CancellationToken cancellationToken)
        {
            var request = CreateRequestMessage(HttpMethod.Get, requestUri);
            var response = await client.SendAsync(request, HttpCompletionOption.ResponseContentRead, cancellationToken);
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            return content;
        }
        
        public static Task<HttpResponseMessage> GetAsync(this IWebClient client, string? requestUri, CancellationToken cancellationToken) => 
            client.GetAsync(CreateUri(requestUri), cancellationToken);
        
        public static Task<HttpResponseMessage> GetAsync(this IWebClient client, Uri? requestUri, CancellationToken cancellationToken) => 
            client.SendAsync(CreateRequestMessage(HttpMethod.Get, requestUri), HttpCompletionOption.ResponseContentRead, cancellationToken);
        
        private static Uri? CreateUri(string? uri) =>
            string.IsNullOrEmpty(uri) ? null : new Uri(uri, UriKind.RelativeOrAbsolute);

        private static HttpRequestMessage CreateRequestMessage(HttpMethod method, Uri? uri) => 
            new(method, uri); //TODO: { Version = _defaultRequestVersion, VersionPolicy = _defaultVersionPolicy };
    }
}