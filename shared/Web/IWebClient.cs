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
        public Uri BaseAddress { get; set; }
        public HttpRequestHeaders DefaultRequestHeaders { get; }

        public Task<HttpResponseMessage> SendAsync(HttpRequestMessage message, HttpCompletionOption option, CancellationToken token);
    }
    
    public interface IWebClientFactory
    {
        IWebClient CreateClient();
    }
    
    public static class WebClientExtensions
    {
        public static Task<HttpResponseMessage> GetAsync(this IWebClient client, string requestUri, CancellationToken cancellationToken) => 
            client.GetAsync(CreateUri(requestUri), cancellationToken);
        public static Task<HttpResponseMessage> GetAsync(this IWebClient client, Uri? requestUri, CancellationToken cancellationToken) => 
            client.SendAsync(new(HttpMethod.Get, requestUri), HttpCompletionOption.ResponseContentRead, cancellationToken);
        private static Uri? CreateUri(string? uri) =>
            string.IsNullOrEmpty(uri) ? null : new Uri(uri, UriKind.RelativeOrAbsolute);
    }
}