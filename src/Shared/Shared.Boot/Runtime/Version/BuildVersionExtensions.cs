namespace Shared.Boot.Version
{
    public static class BuildVersionExtensions
    {
        public static string GetShortDescription(this BuildVersion version)
        {
            return $"{version.Branch} {version.Sha[..7]} | {version.Time:yyyy-MM-dd HH:mm}";
        }
    }
}