#if UNITY_5_6_OR_NEWER
using System;
using System.Runtime.CompilerServices;
using Cysharp.Text;
using Microsoft.Extensions.Logging;
using UnityEngine;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Shared.Log
{
    public class UnityLogger : ILogger
    {
        private class Provider : ILoggerProvider
        {
            void IDisposable.Dispose() { }
            ILogger ILoggerProvider.CreateLogger(string categoryName) => new UnityLogger(categoryName);
        }

        public static void Initialize()
        {
            var factory = LoggerFactory.Create(builder => builder.AddProvider(new Provider()));
            Slog.SetFactory(factory);
            
            var logger = factory.CreateLogger<UnityLogger>();
            logger.Info("complete");
        }

        private readonly string _categoryName;
        private UnityLogger(string categoryName)
        {
            var idx = categoryName.LastIndexOf('.');
            _categoryName = idx >= 0 ? categoryName[(idx + 1)..] : categoryName;
        }

        void ILogger.Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            var logType = ConvertToUnityLogType(logLevel);
            if (state is MsgState msgState)
            {
                // gc-free logger usage (via Shared.Log.LoggerExtensions)
                var sb = ZString.CreateStringBuilder(true);
                try
                {
                    sb.Append(_categoryName);
                    sb.Append(':');
                    sb.Append(' ');
                    msgState.WriteTo(ref sb);
                    if (exception != null)
                    {
                        sb.Append(" : ");
                        sb.Append(exception);
                    }
                    var message = sb.ToString();
                    
                    //TODO: speedup: replace with direct UnityEngine.DebugLogHandler.Internal_Log_Injected usage (spans support)
                    Debug.unityLogger.Log(logType, message);
                }
                finally
                {
                    sb.Dispose();
                }
            }
            else
            {
                // default logger usage
                var message = formatter(state, exception);
                if (exception != null)
                    message = $"{message} : {exception}";
                Debug.unityLogger.Log(logType, _categoryName, message);
            }
        }

        bool ILogger.IsEnabled(LogLevel logLevel) => true; //TODO: setup settings
        IDisposable? ILogger.BeginScope<TState>(TState state) => null;
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static LogType ConvertToUnityLogType(LogLevel level)
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