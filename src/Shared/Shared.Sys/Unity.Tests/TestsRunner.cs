using NUnit.Framework.Interfaces;
using Shared.Log;
using Shared.Sys.Unity.Tests;
using UnityEngine.TestRunner;

[assembly: TestRunCallback(typeof(TestsRunner))]

namespace Shared.Sys.Unity.Tests
{
    public class TestsRunner : ITestRunCallback
    {
        private static readonly Slog.Area _log = new();
        public void RunStarted(ITest testsToRun)
        {
            _log.Info($">>>> {testsToRun.Tests.Count} tests");
            UnitySharedSystem.SetIsRunningTests(true);
        }

        public void RunFinished(ITestResult testResults)
        {
            var resultState = testResults.ResultState;
            _log.Info($"<<<< {resultState} {testResults.Duration:F}s {testResults.FullName} \"{testResults.Message}\"");
            UnitySharedSystem.SetIsRunningTests(false);
        }

        public void TestStarted(ITest test) { }

        public void TestFinished(ITestResult result) { }
    }
}