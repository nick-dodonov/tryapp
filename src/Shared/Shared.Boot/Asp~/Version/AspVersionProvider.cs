using System.Reflection;
using Shared.Boot.Version;

namespace Shared.Boot.Asp.Version;

public class AspVersionProvider : IVersionProvider
{
    //TODO: share with UnityVersionProvider
    private static BuildVersion? _buildVersion;
    public static BuildVersion BuildVersion => _buildVersion ??= Provider.ReadBuildVersion();

    private static IVersionProvider? _provider;
    private static IVersionProvider Provider => _provider ??= new AspVersionProvider();


    BuildVersion IVersionProvider.ReadBuildVersion()
    {
        var assembly = Assembly.GetExecutingAssembly(); //TODO: Assembly.GetEntryAssembly() with SG/task (read .csproj)

        // read from assembly attribute
        var attribute = assembly.GetCustomAttribute<BuildVersionAttribute>();
        if (attribute == null)
            return new();

        var version = new BuildVersion
        {
            Ref = attribute.Ref,
            Sha = attribute.Sha,
        };
        if (DateTime.TryParse(attribute.Timestamp, out var time))
            version.Time = time;

        return version;
    }
}