using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Shared.System
{
    public static class HttpContentExtensions
    {
        /// <summary>
        /// Helper with cancellation token that doesn't exist in mono 
        /// </summary>
        public static Task<string> SharedReadAsStringAsync(this HttpContent content, CancellationToken cancellationToken)
            => SharedSystem.Instance.HttpContent_ReadAsStringAsync(content, cancellationToken);
    }
}