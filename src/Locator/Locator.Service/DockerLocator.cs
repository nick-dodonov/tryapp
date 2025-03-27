using Docker.DotNet;
using Locator.Api;
using Locator.Service.Options;
using Microsoft.Extensions.Options;

namespace Locator.Service;

public class DockerLocator : ILocator
{
    private readonly LocatorConfig _config;
    private readonly DockerClient _dockerClient;

    public DockerLocator(IOptions<LocatorConfig> options, DockerClient dockerClient, ILogger<DockerLocator> logger)
    {
        _dockerClient = dockerClient;
        _config = options.Value;

        logger.LogInformation($"{_config}");
    }

    private const string StackNameKey = "com.docker.stack.namespace";

    public async ValueTask<StandInfo[]> GetStands(CancellationToken cancellationToken)
    {
        var containers = await _dockerClient.Containers.ListContainersAsync(new() { All = true }, cancellationToken);

        var stackPrefix = _config.StandStackPrefix;
        var stands = containers
            .Where(c => 
                c.State == "running" &&
                c.Labels.Any(l =>
                    l.Key == StackNameKey &&
                    l.Value.StartsWith(stackPrefix))
            )
            .Select(c =>
            {
                var name = c.Labels[StackNameKey][stackPrefix.Length..];
                return new StandInfo
                {
                    Name = name,
                    Url = _config.StandUrlTemplate.Replace("$STAND_NAME", name),
                    Created = c.Labels.TryGetValue("org.opencontainers.image.created", out var created) ? created : null,
                    Sha = c.Labels.TryGetValue("org.opencontainers.image.revision", out var sha) ? sha : null,
                };
            })
            .ToArray();

        return stands;
    }
}