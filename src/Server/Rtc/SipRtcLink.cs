using Shared.Log;
using Shared.Session;
using SIPSorcery.Net;

namespace Server.Rtc;

internal class SipRtcLink(string id, RTCPeerConnection peerConnection, ILoggerFactory loggerFactory)
{
    private readonly ILogger _logger = new PeerIdLogger(loggerFactory.CreateLogger<SipRtcLink>(), id);

    public readonly RTCPeerConnection PeerConnection = peerConnection;
    public readonly List<RTCIceCandidate> IceCandidates = [];
    public readonly TaskCompletionSource<List<RTCIceCandidate>> IceCollectCompleteTcs = new();
    public RTCDataChannel? DataChannel;
    public ClientState LastClientState;

    public async Task Init()
    {
        _logger.Info(".");
        DataChannel = await PeerConnection.createDataChannel("test", new()
        {
            ordered = false,
            maxRetransmits = 0
        });
    }

    private class PeerIdLogger(ILogger inner, string peerId) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => inner.BeginScope(state);
        public bool IsEnabled(LogLevel logLevel) => inner.IsEnabled(logLevel);
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel)) return;
            var message = formatter(state, exception);
            var prefixedMessage = $"{message} PeerId={peerId}";
            inner.Log(logLevel, eventId, prefixedMessage, exception, (m, _) => m);
        }
    }
}