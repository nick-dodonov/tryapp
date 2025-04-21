namespace Shared.Boot.Version
{
    public static class BuildInfoExtensions
    {
        public static string GetShortDescription(this BuildInfo info)
        {
            return $"{info.Branch} {info.Sha[..7]} | {info.Time:yyyy-MM-dd HH:mm}";
        }
    }
}