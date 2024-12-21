using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine.Networking;

namespace Shared.Web
{
    /// <summary>
    /// Modified wrapper for http request similar to HttpClient in WebGL
    ///     https://medium.com/@bawahakim/generated-api-client-for-unity-in-c-with-unitywebrequest-78c8418228ba
    ///     https://gist.github.com/bawahakim/69575e656078c6a74f1a6fe75dd02bcd
    /// TODO: add asserts for unsupported features (i.e. HttpCompletionOption etc)
    ///     or possibly simplify interface and don't use HttpClient default request headers at all
    /// </summary>
    public class UnityWebClient : IWebClient
    {
        public UnityWebClient(string baseUri)
        {
            BaseAddress = new(baseUri);
        }

        public UnityWebClient(Uri baseUri)
        {
            BaseAddress = baseUri;
        }

        public Uri BaseAddress { get; set; }
        public HttpRequestHeaders DefaultRequestHeaders => _httpClient.DefaultRequestHeaders;

        private readonly HttpClient _httpClient = new();

        public async Task<HttpResponseMessage> SendAsync(HttpRequestMessage message, HttpCompletionOption option, CancellationToken token)
        {
            var content = await (message.Content?.ReadAsStringAsync() ?? Task.FromResult(""));
            var webRequest = GetUnityWebRequest(message.Method.Method, message.RequestUri, content);

            AppendHeaders(webRequest);

            try
            {
                await webRequest
                    .SendWebRequest()
                    .WithCancellation(cancellationToken: token);
            }
            catch (Exception)
            {
                webRequest.Dispose();
                throw;
            }

            var responseMessage = CreateHttpResponseMessage(webRequest);
            webRequest.Dispose();
            return responseMessage;
        }

        public void Dispose()
        {
            _httpClient.Dispose();
            DefaultRequestHeaders.Clear();
            //BaseAddress = null;
        }

        private UnityWebRequest GetUnityWebRequest(string method, Uri endpoint, string content = "")
        {
            var requestUri = BaseAddress.AbsoluteUri + endpoint;
            var webRequest = UnityWebRequest.Get(requestUri);
            webRequest.method = method;

            webRequest.disposeUploadHandlerOnDispose = true;
            webRequest.disposeDownloadHandlerOnDispose = true;

            if (!string.IsNullOrEmpty(content))
            {
                var data = new System.Text.UTF8Encoding().GetBytes(content);
                webRequest.uploadHandler = new UploadHandlerRaw(data);
                webRequest.SetRequestHeader("Content-Type", "application/json");
            }

            return webRequest;
        }

        private void AppendHeaders(UnityWebRequest webRequest)
        {
            using var enumerator = DefaultRequestHeaders.GetEnumerator();

            while (enumerator.MoveNext())
            {
                var (key, value) = enumerator.Current;
                webRequest.SetRequestHeader(key, value.First());
            }
        }

        private static HttpResponseMessage CreateHttpResponseMessage(UnityWebRequest webRequest)
        {
            var response = new HttpResponseMessage();
            var responseContent = webRequest.downloadHandler?.text;

            response.Content = new StringContent(responseContent);
            response.StatusCode = (HttpStatusCode)webRequest.responseCode;

            foreach (var (key, value) in webRequest.GetResponseHeaders())
            {
                switch (key.ToLower().Trim())
                {
                    case "content-type":
                    {
                        var trimmed = value.ToLower().Split(";").FirstOrDefault();
                        response.Content.Headers.ContentType = new(trimmed);
                        break;
                    }
                    case "content-length":
                        response.Content.Headers.ContentLength = long.Parse(value);
                        break;

                    default:
                        response.Headers.Add(key, value);
                        break;
                }
            }

            return response;
        }
    }
}