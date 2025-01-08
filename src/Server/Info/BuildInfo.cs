using System.Reflection;

namespace Server.Info;

public static class BuildInfo
{
    public static string Timestamp
    {
        get
        {
            var attribute = Assembly.GetExecutingAssembly().GetCustomAttribute<BuildInfoAttribute>();
            return attribute?.Timestamp ?? "<unknown>";
        }
    }
}