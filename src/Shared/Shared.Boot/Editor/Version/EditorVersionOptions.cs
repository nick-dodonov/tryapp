using System.IO;
using Shared.Boot.Version;
using UnityEditor;
using Shared.Log;
using UnityEngine;

namespace Shared.Boot.Editor.Version
{
    public static class EditorVersionOptions
    {
        private const string UseRuntimeProviderMenuItemName = "[Shared]/Version/Use Runtime Provider";
        private static readonly string SessionKey = $"{nameof(EditorVersionOptions)}.{nameof(UseRuntimeProvider)}";

        public static bool UseRuntimeProvider
        {
            get => SessionState.GetBool(SessionKey, false);
            set => SessionState.SetBool(SessionKey, value);
        }

        [MenuItem(UseRuntimeProviderMenuItemName)]
        public static void UseRuntimeProviderToggle() => UseRuntimeProvider = !UseRuntimeProvider;
        [MenuItem(UseRuntimeProviderMenuItemName, true)]
        public static bool UseRuntimeProviderValidate()
        {
            Menu.SetChecked(UseRuntimeProviderMenuItemName, UseRuntimeProvider);
            return !EditorApplication.isPlayingOrWillChangePlaymode;
        }

        [MenuItem("[Shared]/Version/Export Build Info")]
        public static void ExportBuildInfo()
        {
            const string path = "Assets/Resources/BuildInfo.asset";
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            Slog.Info(path);

            var asset = ScriptableObject.CreateInstance<BuildInfoAsset>();
            asset.BuildInfo = ((IVersionProvider)(new EditorVersionProvider())).ReadBuildInfo();

            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
        }

        // private static string GetPackageSourceDir([System.Runtime.CompilerServices.CallerFilePath] string path = "") 
        //     => Path.GetDirectoryName(path)!.Replace("Editor/Version", "");
    }
}