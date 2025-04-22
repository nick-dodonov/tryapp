using UnityEngine;

namespace Shared.Boot.Version
{
    public class UnityVersionProvider : IVersionProvider
    {
        //TODO: share with UnityVersionProvider
        private static BuildVersion? _buildVersion;
        public static BuildVersion BuildVersion => _buildVersion ??= Provider.ReadBuildVersion();

        private static IVersionProvider? _provider;
        private static IVersionProvider Provider => _provider ??= new UnityVersionProvider();
        public static void SetVersionProvider(IVersionProvider provider) => _provider = provider;


        public const string AssetName = nameof(BuildVersion);
        BuildVersion IVersionProvider.ReadBuildVersion()
        {
            var asset = Resources.Load<BuildVersionAsset>(AssetName);
            return asset.buildVersion;
        }
    }
}