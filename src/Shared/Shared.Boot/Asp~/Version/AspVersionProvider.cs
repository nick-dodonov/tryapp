using System.Reflection;
using Shared.Boot.Version;

namespace Shared.Version;

public class AspVersionProvider : IVersionProvider
{
    public BuildVersion ReadBuildVersion()
    {
        var assembly = Assembly.GetExecutingAssembly(); //Assembly.GetEntryAssembly();
        var attribute = assembly.GetCustomAttribute<BuildVersionAttribute>();

        var version = new BuildVersion()
        {
            Sha = "<todo>",
            Branch = "<todo>"
        };
        if (DateTime.TryParse(attribute?.Timestamp, out var time))
            version.Time = time;
        return version;
    }
}