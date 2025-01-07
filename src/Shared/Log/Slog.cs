using System;
using System.Runtime.CompilerServices;
using Cysharp.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using UnityEngine;

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
        public static void Info(string message, [CallerFilePath] string path = "", [CallerMemberName] string member = "")
        {
            var category = new Category(path);
            // gc-free logger usage (via Shared.Log.LoggerExtensions)
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
#if UNITY_5_6_OR_NEWER
                //TODO: speedup: replace with direct UnityEngine.DebugLogHandler.Internal_Log_Injected usage (spans support)
                message = sb.ToString();
                //UnityEngine.Debug.unityLogger.Log(message);
                DebugLogHandler.Internal_Log(LogType.Log, LogOption.None, message);
#else
                //TODO: output with separate initialized ILogger (to get json output too for logging services)
                var span = sb.AsSpan();
                Console.Out.WriteLine(span);
#endif
            }
            finally
            {
                sb.Dispose();
            }
        }
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