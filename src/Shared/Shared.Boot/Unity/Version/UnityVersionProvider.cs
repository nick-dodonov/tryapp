using UnityEngine;

namespace Shared.Boot.Version
{
    public class UnityVersionProvider : IVersionProvider
    {
        public const string AssetName = nameof(BuildVersion);

        private static BuildVersion? _buildVersion;
        public static BuildVersion BuildVersion => _buildVersion ??= VersionProvider.ReadBuildVersion();

        private static IVersionProvider? _versionProvider;
        private static IVersionProvider VersionProvider => _versionProvider ??= new UnityVersionProvider();
        public static void SetVersionProvider(IVersionProvider versionProvider) => _versionProvider = versionProvider;

        BuildVersion IVersionProvider.ReadBuildVersion()
        {
            var asset = Resources.Load<BuildVersionAsset>(AssetName);
            return asset.buildVersion;
        }
    }
}