using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.FileProviders;
using Shared.Boot.Version;
using Shared.Log;

namespace Shared.Boot.Asp.Version;

public class AspVersionProvider : IVersionProvider
{
    private const string BuildVersionResource = "BuildVersion.json";
    private readonly JsonSerializerOptions _jsonSerializerOptions = new JsonSerializerOptions { IncludeFields = true };

    public BuildVersion ReadBuildVersion()
    {
        var assembly = Assembly.GetExecutingAssembly(); //Assembly.GetEntryAssembly();

        // read using embedded resource
        var embeddedProvider = new EmbeddedFileProvider(assembly, "");
        // foreach(var name in assembly.GetManifestResourceNames()) //log all embedded resources
        //     Slog.Info(name);
        // foreach (var fileInfo in embeddedProvider.GetDirectoryContents("")) //log by namespace
        //     Slog.Info(fileInfo.Name);

        using var stream = embeddedProvider.GetFileInfo(BuildVersionResource).CreateReadStream();
        using var reader = new StreamReader(stream);

        var content = reader.ReadToEnd();
        var version = JsonSerializer.Deserialize<BuildVersion>(content, _jsonSerializerOptions);

        // update using assembly attributes
        {
            var attribute = assembly.GetCustomAttribute<BuildVersionAttribute>();
            if (DateTime.TryParse(attribute?.Timestamp, out var time))
                version.Time = time;
        }
        
        return version;
    }
}