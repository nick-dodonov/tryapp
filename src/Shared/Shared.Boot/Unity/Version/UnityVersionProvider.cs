namespace Shared.Boot.Version
{
    public class UnityVersionProvider : IVersionProvider
    {
        private static BuildInfo? _buildInfo;
        public static BuildInfo BuildInfo => _buildInfo ??= VersionProvider.ReadBuildInfo();

        private static IVersionProvider? _versionProvider;
        private static IVersionProvider VersionProvider => _versionProvider ??= new UnityVersionProvider();
        public static void SetVersionProvider(IVersionProvider versionProvider) => _versionProvider = versionProvider;

        BuildInfo IVersionProvider.ReadBuildInfo()
        {
            var asset = UnityEngine.Resources.Load<BuildInfoAsset>("BuildInfo");
            return asset.BuildInfo;
        }
    }
}