#if UNITY_5_6_OR_NEWER
using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine.Networking;

namespace Shared.Web
{
    /// <summary>
    /// TODO: WaitAsync with cancellation
    /// </summary>
    [DebuggerNonUserCode]
    public readonly struct UnityWebRequestAwaiter : INotifyCompletion
    {
        private readonly UnityWebRequestAsyncOperation _asyncOperation;
        public bool IsCompleted => _asyncOperation.isDone;
        public UnityWebRequestAwaiter( UnityWebRequestAsyncOperation asyncOperation ) => _asyncOperation = asyncOperation;
        public void OnCompleted( Action continuation ) => _asyncOperation.completed += _ => continuation();
        public UnityWebRequest GetResult() => _asyncOperation.webRequest;
    }
    public static class ExtensionMethods
    {
        [DebuggerNonUserCode]
        public static UnityWebRequestAwaiter GetAwaiter(this UnityWebRequestAsyncOperation asyncOp) => new(asyncOp);
    }

    /// <summary>
    /// Modified wrapper for http request similar to HttpClient in WebGL
    ///     https://medium.com/@bawahakim/generated-api-client-for-unity-in-c-with-unitywebrequest-78c8418228ba
    ///     https://gist.github.com/bawahakim/69575e656078c6a74f1a6fe75dd02bcd
    /// TODO: add asserts for unsupported features (i.e. HttpCompletionOption etc)
    ///     or possibly simplify interface and don't use HttpClient default request headers at all
    /// </summary>
    public class UnityWebClient : IWebClient
    {
        public UnityWebClient(string baseUri) : this(new Uri(baseUri.TrimEnd('/') + '/')) { }
        private UnityWebClient(Uri baseUri) => BaseAddress = baseUri;

        public Uri BaseAddress { get; set; }

        async Task<HttpResponseMessage> IWebClient.SendAsync(HttpRequestMessage message, HttpCompletionOption option, CancellationToken token)
        {
            var content = await (message.Content?.ReadAsStringAsync() ?? Task.FromResult(""));
            using var request = GetUnityWebRequest(message.Method.Method, message.RequestUri, content);
            await request
                .SendWebRequest()
                //UNITASK: .WithCancellation(cancellationToken: token)
                ;
            var responseMessage = CreateHttpResponseMessage(request);
            return responseMessage;
        }

        async Task<HttpResponseMessage> IWebClient.PostAsync(string uri, string answer, CancellationToken cancellationToken)
        {
            var requestUri = BaseAddress.AbsoluteUri + uri;
            //TODO: mv "application/json" to caller logic
            using var request = UnityWebRequest.Post(requestUri, answer, "application/json");
            await request
                .SendWebRequest()
                //UNITASK: .WithCancellation(cancellationToken)
                ;
            var responseMessage = CreateHttpResponseMessage(request);
            return responseMessage;
        }

        public void Dispose()
        {
            //TODO: cancel and dispose currently opened UnityWebRequest
        }

        private static bool IsError(UnityWebRequest request)
        {
            var result = request.result;
            return result 
                is UnityWebRequest.Result.ConnectionError 
                or UnityWebRequest.Result.DataProcessingError 
                or UnityWebRequest.Result.ProtocolError;
        }
        
        private UnityWebRequest GetUnityWebRequest(string method, Uri endpoint, string content = "")
        {
            var requestUri = BaseAddress.AbsoluteUri + endpoint;
            var webRequest = UnityWebRequest.Get(requestUri);
            webRequest.method = method;

            webRequest.disposeUploadHandlerOnDispose = true;
            webRequest.disposeDownloadHandlerOnDispose = true;

            //TODO: move "application/json" setup to caller
            if (!string.IsNullOrEmpty(content))
            {
                var data = new System.Text.UTF8Encoding().GetBytes(content);
                webRequest.uploadHandler = new UploadHandlerRaw(data);
                webRequest.SetRequestHeader("Content-Type", "application/json");
            }

            return webRequest;
        }

        private static readonly StringContent _emptyStringContent = new(string.Empty);
        private static HttpResponseMessage CreateHttpResponseMessage(UnityWebRequest request)
        {
            if (IsError(request))
                throw new UnityWebRequestException(request);
            
            var response = new HttpResponseMessage();
            response.StatusCode = (HttpStatusCode)request.responseCode;

            var responseContent = request.downloadHandler?.text;
            response.Content = responseContent != null 
                ? new(responseContent) 
                : _emptyStringContent;

            var responseHeaders = request.GetResponseHeaders();
            if (responseHeaders != null)
            {
                foreach (var (key, value) in responseHeaders)
                {
                    var keyLower = key.ToLower().Trim();
                    switch (keyLower)
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
            }

            return response;
        }
    }
}
#endif
