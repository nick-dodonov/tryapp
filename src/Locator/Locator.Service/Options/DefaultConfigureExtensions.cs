namespace Locator.Service.Options;

public static class DefaultConfigureExtensions
{
    /// <summary>
    /// Helper to set up options with by default section name and validations
    /// </summary>
    public static IServiceCollection DefaultConfigure<TOptions>(this IServiceCollection services, IConfigurationRoot configRoot) where TOptions : class
    {
        services
            .AddOptions<TOptions>()
            .Bind(configRoot.GetSection(typeof(TOptions).Name))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        return services;
    }
}