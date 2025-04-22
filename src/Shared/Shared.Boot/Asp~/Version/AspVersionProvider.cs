using System.Reflection;
using Shared.Boot.Version;

namespace Shared.Boot.Asp.Version;

public class AspVersionProvider : IVersionProvider
{
    public BuildVersion ReadBuildVersion()
    {
        var assembly = Assembly.GetExecutingAssembly(); //TODO: Assembly.GetEntryAssembly() with SG/task (read .csproj)

        // read from assembly attribute
        var attribute = assembly.GetCustomAttribute<BuildVersionAttribute>();
        if (attribute == null)
            return new();

        var version = new BuildVersion
        {
            Sha = attribute.Sha,
            Branch = attribute.Branch,
        };
        if (DateTime.TryParse(attribute.Timestamp, out var time))
            version.Time = time;

        return version;
    }
}