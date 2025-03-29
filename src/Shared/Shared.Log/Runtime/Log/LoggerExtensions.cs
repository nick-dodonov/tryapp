using System;
using System.Runtime.CompilerServices;
using Cysharp.Text;
using Microsoft.Extensions.Logging;

namespace Shared.Log
{
    /// <summary>
    /// Helpers with auto member addition and custom state/formatter to implement gc-free logging with unity logger
    /// </summary>
    public static class LoggerExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Write(this ILogger logger, LogLevel logLevel, string message, [CallerMemberName] string member = "") 
            => logger.Log(logLevel, 0, new(message, member), null, MsgState.Formatter);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Info(this ILogger logger, string message, [CallerMemberName] string member = "") 
            => logger.Log(LogLevel.Information, 0, new(message, member), null, MsgState.Formatter);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Warn(this ILogger logger, string message, [CallerMemberName] string member = "") 
            => logger.Log(LogLevel.Warning, 0, new(message, member), null, MsgState.Formatter);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Error(this ILogger logger, string message, [CallerMemberName] string member = "") 
            => logger.Log(LogLevel.Error, 0, new(message, member), null, MsgState.Formatter);
    }
    
    /// <summary>
    /// Custom state implementing $"{member}: {message}" gc-free interpolation
    /// </summary>
    internal readonly struct MsgState
    {
        public readonly string Message;
        public readonly string Member;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public MsgState(string message, string member)
        {
            Message = message;
            Member = member;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override string ToString()
        {
            return $"{Member}: {Message}";
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteTo(ref Utf16ValueStringBuilder sb)
        {
            sb.Append(Member);
            sb.Append(':');
            sb.Append(' ');
            sb.Append(Message);
        }

        internal static readonly Func<MsgState, Exception?, string> Formatter = MsgFormatter;
        private static string MsgFormatter(MsgState state, Exception? error) => state.ToString();
    }
    
    /// <summary>
    /// Helper to be used for less gc-pressure in Slog ASP impl
    /// TODO: possible shared array usage to get rid of gc completely 
    /// </summary>
    internal readonly struct MsgStaticState
    {
        private readonly string _message;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public MsgStaticState(string message) => _message = message;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override string ToString() => _message;

        internal static readonly Func<MsgStaticState, Exception?, string> Formatter = MsgFormatter;
        private static string MsgFormatter(MsgStaticState state, Exception? error) => state.ToString();
    }
}