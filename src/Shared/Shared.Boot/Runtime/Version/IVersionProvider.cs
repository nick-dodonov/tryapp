namespace Shared.Boot.Version
{
    public interface IVersionProvider
    {
        BuildVersion ReadBuildVersion();
    }
}