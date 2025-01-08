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
        public static void Info(this ILogger logger, string message, [CallerMemberName] string member = "") 
            => logger.Log(LogLevel.Information, 0, new(message, member), null, _msgFormatter);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Warn(this ILogger logger, string message, [CallerMemberName] string member = "") 
            => logger.Log(LogLevel.Warning, 0, new(message, member), null, _msgFormatter);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Error(this ILogger logger, string message, [CallerMemberName] string member = "") 
            => logger.Log(LogLevel.Error, 0, new(message, member), null, _msgFormatter);

        private static readonly Func<MsgState, Exception?, string> _msgFormatter = MsgFormatter;
        private static string MsgFormatter(MsgState state, Exception? error) => state.ToString();
    }
    
    /// <summary>
    /// Custom state implementing $"{member}: {message}" gc-free interpolation
    /// </summary>
    internal readonly struct MsgState
    {
        private readonly string _message;
        private readonly string _member;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public MsgState(string message, string member)
        {
            _message = message;
            _member = member;
        }
            
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override string ToString()
        {
            return $"{_member}: {_message}";
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteTo(ref Utf16ValueStringBuilder sb)
        {
            sb.Append(_member);
            sb.Append(':');
            sb.Append(' ');
            sb.Append(_message);
        }
    }
}