using System;
using Microsoft.Extensions.Logging;

namespace Shared.Tp.Rtc.Sip
{
    internal class SipLinkLogger : ILogger
    {
        private readonly ILogger _inner;
        private readonly string _linkId;

        public SipLinkLogger(ILogger inner, string linkId)
        {
            _inner = inner;
            _linkId = linkId;
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => _inner.BeginScope(state);
        public bool IsEnabled(LogLevel logLevel) => _inner.IsEnabled(logLevel);

        public void Log<TState>(
            LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel)) return;
            var message = formatter(state, exception);
            var prefixedMessage = $"<{_linkId}> {message}";
            _inner.Log(logLevel, eventId, prefixedMessage, exception, (m, _) => m);
        }
    }
}