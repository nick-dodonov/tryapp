using System.Reflection;

namespace Shared.Audit;

public static class BuildInfo
{
    public static string Timestamp
    {
        get
        {
            var assembly = Assembly.GetExecutingAssembly(); //Assembly.GetEntryAssembly();
            var attribute = assembly.GetCustomAttribute<BuildInfoAttribute>();
            return attribute?.Timestamp ?? "<unknown>";
        }
    }
}