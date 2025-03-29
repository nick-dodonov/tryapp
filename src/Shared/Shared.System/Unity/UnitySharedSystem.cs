using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Shared.System
{
    public class UnitySharedSystem : ISharedSystem
    {
        [UnityEditor.InitializeOnLoadMethod]
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        private static void Initialize()
        {
            SharedSystem.SetInstance(new UnitySharedSystem());
        }

        public Task<string> HttpContent_ReadAsStringAsync(HttpContent content, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return content.ReadAsStringAsync();
        }
    }
}