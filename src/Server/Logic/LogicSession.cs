using System.Collections.Concurrent;
using Common.Logic;
using Shared.Log;
using Shared.Tp;

namespace Server.Logic;

public class LogicSession(ILoggerFactory loggerFactory, ITpApi tpApi)
    : IHostedService, ITpListener, ITpReceiver
{
    private readonly ILogger _logger = loggerFactory.CreateLogger<LogicSession>();

    private readonly ConcurrentDictionary<ITpLink, LogicPeer> _peers = new();

    Task IHostedService.StartAsync(CancellationToken cancellationToken)
    {
        _logger.Info("start listening");
        tpApi.Listen(this);
        return Task.CompletedTask;
    }

    Task IHostedService.StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    ITpReceiver ITpListener.Connected(ITpLink link)
    {
        _logger.Info($"{link}");
        var peer = new LogicPeer(loggerFactory.CreateLogger<LogicPeer>(), this, link);
        _peers.TryAdd(link, peer);
        return this;
    }

    void ITpReceiver.Received(ITpLink link, byte[]? bytes)
    {
        var linkId = link.GetRemotePeerId();
        if (!_peers.TryGetValue(link, out var peer))
        {
            var msg = $"{(bytes == null ? "disconnected" : $"[{bytes.Length}] bytes")}";
            _logger.Warn($"peer not found: {linkId} ({msg})");
            return;
        }

        if (bytes == null)
        {
            _logger.Info($"peer disconnected: {linkId}");
            if (_peers.TryRemove(link, out peer))
                peer.Dispose();
            return;
        }

        peer.Received(bytes);
    }

    public ServerState GetServerState(int frame)
    {
        var utcMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var peerStates = _peers
            .Select(x => x.Value.GetPeerState())
            .ToArray();
        return new()
        {
            Frame = frame,
            UtcMs = utcMs,
            Peers = peerStates
        };
    }
}