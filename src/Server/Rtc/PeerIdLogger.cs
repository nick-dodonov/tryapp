namespace Server.Rtc;

internal class PeerIdLogger(ILogger inner, string peerId) : ILogger
{
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => inner.BeginScope(state);
    public bool IsEnabled(LogLevel logLevel) => inner.IsEnabled(logLevel);

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel)) return;
        var message = formatter(state, exception);
        var prefixedMessage = $"{message} PeerId={peerId}";
        inner.Log(logLevel, eventId, prefixedMessage, exception, (m, _) => m);
    }
}