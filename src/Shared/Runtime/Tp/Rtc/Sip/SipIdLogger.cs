using System;
using Microsoft.Extensions.Logging;

namespace Server.Rtc
{
    internal class SipIdLogger : ILogger
    {
        private readonly ILogger _inner;
        private readonly string _peerId;

        public SipIdLogger(ILogger inner, string peerId)
        {
            _inner = inner;
            _peerId = peerId;
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => _inner.BeginScope(state);
        public bool IsEnabled(LogLevel logLevel) => _inner.IsEnabled(logLevel);

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel)) return;
            var message = formatter(state, exception);
            var prefixedMessage = $"{message} PeerId={_peerId}";
            _inner.Log(logLevel, eventId, prefixedMessage, exception, (m, _) => m);
        }
    }
}