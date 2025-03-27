using Docker.DotNet;
using Locator.Api;

namespace Locator.Service;

public class DockerLocator(DockerClient dockerClient) : ILocator
{
    private const string StandNamespacePrefix = "stand-";

    public async ValueTask<StandInfo[]> GetStands(CancellationToken cancellationToken)
    {
        var containers = await dockerClient.Containers.ListContainersAsync(new() { All = true }, cancellationToken);
        var stands = containers
            .Where(c => c.State == "running")
            // .Where(c => c.Labels.Any(l =>
            //     l.Key == "com.docker.stack.namespace" &&
            //     l.Value.StartsWith(StandNamespacePrefix))
            // )
            .SelectMany(c => c.Labels)
            .Where(l =>
                l.Key == "com.docker.stack.namespace" &&
                l.Value.StartsWith(StandNamespacePrefix))
            .Select(l => new StandInfo
            {
                Name = l.Value[StandNamespacePrefix.Length..]
            });
        return stands.ToArray();
    }
}