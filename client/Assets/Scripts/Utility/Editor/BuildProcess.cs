using System;
using System.Diagnostics;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using Debug = UnityEngine.Debug;

namespace Utility.Editor
{
    public class BuildProcess : IPreprocessBuildWithReport
    {
        [InitializeOnLoadMethod]
        private static void InitOnLoad()
        {
            BuildPlayerWindow.RegisterBuildPlayerHandler(OnBuildPlayer);
        }

        private static void OnBuildPlayer(BuildPlayerOptions options)
        {
            var buildOptions = options.options;
            Debug.Log($"BuildProcess: OnBuildPlayer: {buildOptions}");
            
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
                Debug.Log($"BuildProcess: OnBuildPlayer: just open browser (VSCode Five Server hosting): {url}");
                var process = new Process();
                process.StartInfo.FileName = url;
                process.Start();
            }
        }

        int IOrderedCallback.callbackOrder => 0;
        void IPreprocessBuildWithReport.OnPreprocessBuild(BuildReport report)
        {
            Debug.Log($"BuildProcess: OnPreprocessBuild: {report.summary.outputPath}");
        }
    }
}