using System;
using System.Runtime.CompilerServices;
using Cysharp.Text;
using Microsoft.Extensions.Logging;
using UnityEngine;

namespace Shared.Log
{
    /// <summary>
    /// Static logger (useful for quick usage without additional setup in ASP or in shared with client code)
    /// </summary>
    public static class Slog
    {
        public interface IInitializer
        {
            ILoggerFactory CreateDefaultFactory();
        }
        private static readonly IInitializer _initializer =
#if UNITY_5_6_OR_NEWER
            new Unity.UnitySlogInitializer();
#else
            new Asp.AspSlogInitializer();
#endif

        public static ILoggerFactory Factory { get; } = _initializer.CreateDefaultFactory();
#if !UNITY_5_6_OR_NEWER
        private static readonly ILogger _staticLogger = Factory.CreateLogger("SLOG");
#endif

        [HideInCallstack, MethodImpl(MethodImplOptions.AggressiveInlining)]
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
#if UNITY_5_6_OR_NEWER
                var logType = Unity.UnityLogger.ConvertToUnityLogType(level);

                // message = sb.ToString();
                // UnityEngine.Debug.unityLogger.Log(logType, message);
                // UnityEngine.DebugLogHandler.Internal_Log(logType, UnityEngine.LogOption.None, message);

                var span = sb.AsSpan();
                DebugLogHandler.Internal_Log(logType, LogOption.None, span);
#else
                // var span = sb.AsSpan();
                // Console.Out.WriteLine(span);

                //output with separate ILogger (to get json output too for logging services)
                //TODO: possible shared array usage to get rid of gc completely
                _staticLogger.Log(level, 0, new(sb.ToString()), null, MsgStaticState.Formatter);
#endif
            }
            finally
            {
                sb.Dispose();
            }
        }

        [HideInCallstack, MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Info(string message, [CallerFilePath] string path = "", [CallerMemberName] string member = "") 
            => Write(LogLevel.Information, message, new(path), member);
        [HideInCallstack, MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Warn(string message, [CallerFilePath] string path = "", [CallerMemberName] string member = "") 
            => Write(LogLevel.Warning, message, new(path), member);
        [HideInCallstack, MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Error(string message, [CallerFilePath] string path = "", [CallerMemberName] string member = "") 
            => Write(LogLevel.Error, message, new(path), member);

        /// <summary>
        /// Helper for unity's static loggin
        /// </summary>
        public class Area
        {
            private readonly Category _category;
            public Area(string? customName = null, [CallerFilePath] string path = "")
            {
                _category = customName != null 
                    ? new(customName) 
                    : new(path);
            }

            [HideInCallstack, MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Info(string message, [CallerMemberName] string member = "") 
                => Write(LogLevel.Information, message, _category, member);
            [HideInCallstack, MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Warn(string message, [CallerMemberName] string member = "") 
                => Write(LogLevel.Warning, message, _category, member);
            [HideInCallstack, MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Error(string message, [CallerMemberName] string member = "") 
                => Write(LogLevel.Error, message, _category, member);
        }
    }

    public readonly struct Category
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