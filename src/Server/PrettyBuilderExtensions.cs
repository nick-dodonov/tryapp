using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging.Console;

namespace Server;

public static class PrettyBuilderExtensions
{
    public static IHostApplicationBuilder AddPrettyConsoleLoggerProvider(this IHostApplicationBuilder builder)
    {
        foreach (var service in builder.Services)
        {
            if (service.ImplementationType != typeof(ConsoleLoggerProvider))
                continue;
            builder.Services.Remove(service);
            break;
        }

        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, PrettyConsoleLoggerProvider>());
        return builder;
    }
}