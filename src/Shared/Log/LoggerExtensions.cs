using System;
using System.Runtime.CompilerServices;
using Cysharp.Text;
using Microsoft.Extensions.Logging;

namespace Shared.Log
{
    public static class LoggerExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Info(this ILogger logger, string message, [CallerMemberName] string member = "")
        {
            //logger.Log(LogLevel.Information, $"{member}: {message}");
            
            //via custom state and formatter to implement gc-free logging with unity logger
            logger.Log(
                LogLevel.Information,
                0,
                new(message, member),
                null,
                _msgFormatter);
        }

        private static readonly Func<MsgState, Exception?, string> _msgFormatter = MsgFormatter;
        private static string MsgFormatter(MsgState state, Exception? error) => state.ToString();
    }
    
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