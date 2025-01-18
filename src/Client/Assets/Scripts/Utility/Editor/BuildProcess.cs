using System.Diagnostics;
using Shared.Log;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using Debug = UnityEngine.Debug;

namespace Client.Utility.Editor
{
    public class BuildProcess : IPreprocessBuildWithReport
    {
        private static readonly Slog.Area _log = new();
        
        [InitializeOnLoadMethod]
        private static void InitOnLoad()
        {
            BuildPlayerWindow.RegisterBuildPlayerHandler(OnBuildPlayer);
        }

        private static void OnBuildPlayer(BuildPlayerOptions options)
        {
            var buildOptions = options.options;
            _log.Info($"{buildOptions}");

            // replace default webgl build hosting service
            //  there is the issue with WebRTC usage because of several response headers miss 
            var autoRun = (buildOptions & BuildOptions.AutoRunPlayer) != 0;
            if (autoRun)
                buildOptions &= ~BuildOptions.AutoRunPlayer;
            options.options = buildOptions;

            BuildPlayerWindow.DefaultBuildMethods.BuildPlayer(options);

            if (autoRun)
            {
                //TODO: different browsers or hosting variants / start hosting / etc
                const string url = "http://localhost:5500/build/WebGL/local/index.html";
                _log.Info($"opening browser (VSCode 'Five Server' hosting): {url}");
                var process = new Process();
                process.StartInfo.FileName = url;
                process.Start();
            }
        }

        int IOrderedCallback.callbackOrder => 0;
        void IPreprocessBuildWithReport.OnPreprocessBuild(BuildReport report)
        {
            var summary = report.summary;
            _log.Info(@$"{summary.outputPath}
platform: {summary.platform}
buildType: {summary.buildType} 
options: ""{summary.options}""");
        }
    }
}