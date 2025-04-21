namespace Shared.Boot.Version
{
    public interface IVersionProvider
    {
        BuildInfo ReadBuildInfo();
    }
}