namespace Locator.Service;

public class DockerConfig
{
    // Possible values:
    // * any allowed in https://github.com/dotnet/Docker.DotNet/#usage
    // * relative docker.sock (local debug of remote docker over ssh, look setup-remote-ssh-docker-sock.sh) 
    // * default when unset
    public string? Url { get; init; }
}