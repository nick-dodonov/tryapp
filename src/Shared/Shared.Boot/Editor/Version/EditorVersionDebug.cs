using Shared.Boot.Version;
using Shared.Log;
using UnityEditor;

namespace Shared.Boot.Editor.Version
{
    public static class EditorVersionDebug
    {
        [MenuItem("[Shared]/Version/Debug/Write Build Version")]
        public static void WriteBuildVersion()
            => EditorVersionExporter.ExportBuildVersion();

        [MenuItem("[Shared]/Version/Debug/Read Build Version")]
        public static void ReadBuildVersion()
        {
            var buildVersion = ((IVersionProvider)(new UnityVersionProvider())).ReadBuildVersion();
            Slog.Info(buildVersion.ToShortInfo());
        }
    }
}