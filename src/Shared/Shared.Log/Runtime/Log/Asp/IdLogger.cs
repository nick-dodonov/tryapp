using System;
using Cysharp.Text;
using Microsoft.Extensions.Logging;

namespace Shared.Log.Asp
{
    /// <summary>
    /// Helper to simplify logging of specific peers
    /// TODO: speedup with custom formatter instead of string interpolation
    /// </summary>
    public class IdLogger : ILogger
    {
        private readonly ILogger _inner;
        private readonly string _prefix;

        public IdLogger(ILogger inner, string id)
        {
            _inner = inner;
            _prefix = $"<{id}> ";
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => _inner.BeginScope(state);
        public bool IsEnabled(LogLevel logLevel) => _inner.IsEnabled(logLevel);

        public void Log<TState>(
            LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
                return;
            var sb = ZString.CreateStringBuilder();
            try
            {
                sb.Append(_prefix);
                if (state is MsgState msgState)
                    msgState.WriteTo(ref sb);
                else
                    sb.Append(formatter(state, exception));
                _inner.Log(logLevel, eventId, sb, exception, Formatter);
            }
            finally
            {
                sb.Dispose();
            }
        }

        private static readonly Func<Utf16ValueStringBuilder, Exception?, string> Formatter = MsgFormatter;
        private static string MsgFormatter(Utf16ValueStringBuilder state, Exception? error) => state.ToString();
    }
}