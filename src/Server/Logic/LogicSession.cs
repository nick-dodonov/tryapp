using System.Collections.Concurrent;
using Common.Logic;
using Shared.Log;
using Shared.Log.Asp;
using Shared.Tp;
using Shared.Tp.Ext.Misc;

namespace Server.Logic;

public class LogicSession(ILoggerFactory loggerFactory, ITpApi tpApi)
    : IHostedService, ITpListener, ITpReceiver
{
    private readonly ILogger _logger = loggerFactory.CreateLogger<LogicSession>();

    private readonly TimeLink.Api _timeApi = tpApi.Find<TimeLink.Api>() ?? throw new("TimeLink.Api not found");
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
        var peer = new LogicPeer(
            new IdLogger(loggerFactory.CreateLogger<LogicPeer>(), link.GetRemotePeerId()),
            this, link);
        _peers.TryAdd(link, peer);
        return this;
    }

    void ITpReceiver.Received(ITpLink link, ReadOnlySpan<byte> span)
    {
        if (_peers.TryGetValue(link, out var peer))
            peer.Received(span);
        else
            _logger.Warn($"peer not found: {link} ([{span.Length}] bytes)");
    }

    void ITpReceiver.Disconnected(ITpLink link)
    {
        if (_peers.TryRemove(link, out var peer))
        {
            _logger.Info($"peer disconnected: {link}");
            peer.Dispose();
        }
        else 
            _logger.Warn($"peer not found: {link}");
    }
    
    public ServerState GetServerState(int frame)
    {
        var utcMs = _timeApi.LocalMs;
        var peerStates = _peers
            .Select(x => x.Value.GetPeerState())
            .ToArray();
        return new()
        {
            Frame = frame,
            SesMs = utcMs,
            Peers = peerStates
        };
    }
}