using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Scripting;

[assembly: AlwaysLinkAssembly]

namespace Shared.Sys
{
    public class UnitySharedSystem : ISharedSystem
    {
#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
#endif
        [Preserve, RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        private static void Initialize()
        {
            SharedSystem.SetInstance(new UnitySharedSystem());
        }

        public Task<string> HttpContent_ReadAsStringAsync(HttpContent content, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return content.ReadAsStringAsync();
        }

        public static bool IsRunningTests
        {
            get
            {
#if !UNITY_INCLUDE_TESTS
                return false;
#else
                // Unfortunately, TestsRunner setup _isRunningTests is too late.
                // It doesn't allow using IsRunningTests in RuntimeInitializeOnLoadMethod, 
                //  so we have to detect tests execution here.
                return _isRunningTests || DetectedRunningTests;
#endif
            }
        }

#if UNITY_INCLUDE_TESTS
        private static bool _isRunningTests;
        public static void SetIsRunningTests(bool isRunningTests) => _isRunningTests = isRunningTests;

        private static bool? _detectedRunningTests;
        private static bool DetectedRunningTests => _detectedRunningTests ??= DetectRunningTests();
        private static bool DetectRunningTests()
        {
            var activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (activeScene.name.StartsWith("InitTestScene"))
                return true;
            return false;
        }
#endif
    }
}