using System.IO;
using Shared.Boot.Version;
using Shared.Log;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Shared.Boot.Editor.Version
{
    public class EditorVersionExporter : IPreprocessBuildWithReport
    {
        private static readonly string AssetPath = $"Assets/Resources/{UnityVersionProvider.AssetName}.asset";

        int IOrderedCallback.callbackOrder { get; }
        void IPreprocessBuildWithReport.OnPreprocessBuild(BuildReport report) => ExportBuildVersion();

        public static void ExportBuildVersion()
        {
            var path = AssetPath;
            Slog.Info(path);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);

            var asset = ScriptableObject.CreateInstance<BuildVersionAsset>();
            asset.buildVersion = ((IVersionProvider)(new EditorVersionProvider())).ReadBuildVersion();

            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
        }
    }
}