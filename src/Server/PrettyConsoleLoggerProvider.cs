using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;

namespace Server;

/// <summary>
/// Helper to make console logger category shorter (without namespaces of specific loggers)
/// TODO: register pretty console formatter to colorize category and remove event id
/// </summary>
public class PrettyConsoleLoggerProvider : ConsoleLoggerProvider, ILoggerProvider
{
    private static readonly string[] _prefixes = ["Shared", "Server"];

    public PrettyConsoleLoggerProvider(
        IOptionsMonitor<ConsoleLoggerOptions> options)
        : base(options) { }

    public PrettyConsoleLoggerProvider(
        IOptionsMonitor<ConsoleLoggerOptions> options,
        IEnumerable<ConsoleFormatter>? formatters)
        : base(options, formatters) { }

    ILogger ILoggerProvider.CreateLogger(string categoryName)
    {
        if (_prefixes.Any(prefix => categoryName.StartsWith(prefix)))
        {
            var idx = categoryName.LastIndexOf('.');
            if (idx >= 0)
                categoryName = categoryName[(idx + 1)..];
        }

        return base.CreateLogger(categoryName);
    }
}