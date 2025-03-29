using System;
using Microsoft.Extensions.Options;

namespace Shared.Options
{
    /// <summary>
    /// Helper to wrap existing options value to monitor for using in shared code
    /// </summary>
    public class StaticOptionsMonitor<TOptions> : IOptionsMonitor<TOptions> {
        public StaticOptionsMonitor(in TOptions value) => CurrentValue = value;

        public TOptions CurrentValue { get; }
        TOptions IOptionsMonitor<TOptions>.Get(string? name) => CurrentValue;
        IDisposable? IOptionsMonitor<TOptions>.OnChange(Action<TOptions, string?> listener) => null;
    }
}