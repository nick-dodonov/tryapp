#if UNITY_5_6_OR_NEWER
using System;
using System.Runtime.CompilerServices;
using Cysharp.Text;
using Microsoft.Extensions.Logging;
using UnityEngine;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Shared.Log.Unity
{
    /// <summary>
    /// Adapter allowing to use Microsoft.Extensions.Logging in Unity
    /// </summary>
    public class UnityLogger : ILogger
    {
        internal class Provider : ILoggerProvider
        {
            void IDisposable.Dispose()
            {
            }

            ILogger ILoggerProvider.CreateLogger(string categoryName) => new UnityLogger(categoryName);
        }

        private readonly string _categoryName;

        private UnityLogger(string categoryName)
        {
            var idx = categoryName.LastIndexOf('.');
            _categoryName = idx >= 0 ? categoryName[(idx + 1)..] : categoryName;
        }

        void ILogger.Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            var sb = ZString.CreateStringBuilder(true);
            try
            {
                Slog.WriteCategoryPrefix(ref sb, _categoryName, '\u24C2');

                // gc-free logger usage (via Shared.Log.LoggerExtensions or specific loggers like IdLogger)
                switch (state)
                {
                    case MsgState msgState:
                        msgState.WriteTo(ref sb);
                        break;
                    case Utf16ValueStringBuilder sbState:
                        sb.Append(sbState.AsSpan());
                        break;
                    case string strState:
                        sb.Append(strState);
                        break;
                    default: // default logger usage via formatter
                        sb.Append(formatter(state, exception));
                        break;
                }

                if (exception != null)
                {
                    sb.Append(" : ");
                    sb.Append(exception);
                }

                var logType = ConvertToUnityLogType(logLevel);
                var span = sb.AsSpan();
                DebugLogHandler.Internal_Log(logType, LogOption.None, span);
            }
            finally
            {
                sb.Dispose();
            }
        }

        bool ILogger.IsEnabled(LogLevel logLevel) => true; //TODO: setup settings
        IDisposable? ILogger.BeginScope<TState>(TState state) => null;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static LogType ConvertToUnityLogType(LogLevel level)
        {
            return level switch
            {
                LogLevel.Trace => LogType.Log,
                LogLevel.Debug => LogType.Log,
                LogLevel.Information => LogType.Log,
                LogLevel.Warning => LogType.Warning,
                LogLevel.Error => LogType.Error,
                LogLevel.Critical => LogType.Error,
                LogLevel.None => LogType.Assert,
                _ => throw new ArgumentOutOfRangeException(nameof(level), level, null)
            };
        }
    }
}
#endif