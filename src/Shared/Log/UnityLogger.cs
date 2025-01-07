using System;
using System.Runtime.CompilerServices;
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
            var message = formatter(state, exception);
            // if (message.IndexOf('\n', StringComparison.Ordinal) >= 0)
            //     message = message.Replace(Environment.NewLine, " ");
            if (exception != null)
                message = $"{message} : {exception}";
            
            var logType = ConvertToUnityLogType(logLevel);
            Debug.unityLogger.Log(logType, _categoryName, message);
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