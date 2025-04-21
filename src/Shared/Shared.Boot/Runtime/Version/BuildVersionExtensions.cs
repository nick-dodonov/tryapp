namespace Shared.Boot.Version
{
    public static class BuildVersionExtensions
    {
        public static string GetShortDescription(this BuildVersion version)
        {
            var sha = version.Sha;
            return $"{version.Branch} | {(sha.Length > 7 ? sha[..7]: sha)} | {version.Time:yyyy-MM-dd HH:mm}";
        }
    }
}