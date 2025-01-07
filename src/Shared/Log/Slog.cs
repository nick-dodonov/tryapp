using System;
using System.Runtime.CompilerServices;
using Cysharp.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Shared.Log
{
    /// <summary>
    /// Static logger (useful for quick usage without additional setup in ASP or in shared with client code)
    /// </summary>
    public static class Slog
    {
        public static ILoggerFactory Factory { get; private set; } = NullLoggerFactory.Instance;
        public static void SetFactory(ILoggerFactory factory) => Factory = factory;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Write(LogLevel level, string message, in Category category, string member)
        {
            // gc-free logger usage
            var sb = ZString.CreateStringBuilder(true);
            try
            {
                sb.Append(category.NameSpan);
                sb.Append(':');
                sb.Append(' ');
                sb.Append(member);
                sb.Append(':');
                sb.Append(' ');
                sb.Append(message);

                var span = sb.AsSpan();
#if UNITY_5_6_OR_NEWER
                var logType = UnityLogger.ConvertToUnityLogType(level);
                
                // message = sb.ToString();
                // UnityEngine.Debug.unityLogger.Log(logType, message);
                // UnityEngine.DebugLogHandler.Internal_Log(logType, UnityEngine.LogOption.None, message);

                UnityEngine.DebugLogHandler.Internal_Log(logType, UnityEngine.LogOption.None, span);
#else
                //TODO: output with separate initialized ILogger (to get json output too for logging services)
                Console.Out.WriteLine(span);
#endif
            }
            finally
            {
                sb.Dispose();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Info(string message, [CallerFilePath] string path = "", [CallerMemberName] string member = "") 
            => Write(LogLevel.Information, message, new(path), member);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Warn(string message, [CallerFilePath] string path = "", [CallerMemberName] string member = "") 
            => Write(LogLevel.Warning, message, new(path), member);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Error(string message, [CallerFilePath] string path = "", [CallerMemberName] string member = "") 
            => Write(LogLevel.Error, message, new(path), member);
    }

    public readonly ref struct Category
    {
        private static readonly char[] PathSeparators = { '/', '\\' };
        private readonly string _path;
        private readonly int _start;
        private readonly int _length;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Category(string path)
        {
            _path = path;
            _start = path.LastIndexOfAny(PathSeparators);
            ++_start; //excess slash is trimmed and not found case is handled too
            var end = path.LastIndexOf('.');
            _length = end >= 0 
                ? end - _start 
                : path.Length - _start;
        }

        public ReadOnlySpan<char> NameSpan => _path.AsSpan(_start, _length);
    }
}