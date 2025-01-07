using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

namespace Shared.Log
{
    public static class LoggerExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Info(this ILogger logger, string message, [CallerMemberName] string member = null!) 
            => logger.Log(LogLevel.Information, $"{member}: {message}");
    }
}