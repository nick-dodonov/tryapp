using System.Collections.Concurrent;
using Common.Logic;
using Shared.Log;
using Shared.Log.Asp;
using Shared.Tp;
using Shared.Tp.Ext.Misc;

namespace Server.Logic;

public class ServerSession(ILoggerFactory loggerFactory, ITpApi tpApi)
    : IHostedService, ITpListener, ITpReceiver
{
    private readonly ILogger _logger = loggerFactory.CreateLogger<ServerSession>();

    private readonly TimeLink.Api _timeApi = tpApi.Find<TimeLink.Api>() ?? throw new("TimeLink.Api not found");
    private readonly ConcurrentDictionary<ITpLink, ServerPeer> _peers = new();

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
        var peer = new ServerPeer(
            new IdLogger(loggerFactory.CreateLogger<ServerPeer>(), link.GetRemotePeerId()),
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
        var sessionMs = _timeApi.LocalMs;
        var peerStates = _peers
            .Select(x => x.Value.GetPeerState())
            .Append(GetVirtualPeerState(frame, sessionMs))
            .ToArray();
        return new()
        {
            Frame = frame,
            Ms = sessionMs,
            Peers = peerStates
        };
    }

    private PeerState GetVirtualPeerState(int frame, int sessionMs)
    {
        const float radius = 0.8f;
        const int circleTimeMs = 60_000;

        var angle = (float)(2 * Math.PI * (sessionMs % circleTimeMs) / circleTimeMs);
        var x = -radius * MathF.Cos(angle);
        var y = radius * MathF.Sin(angle);
        return new()
        {
            Id = "virtual",
            ClientState = new()
            {
                Frame = frame,
                Ms = sessionMs,
                X = x,
                Y = y,
                Color = 0x7F7F7F
            }
        };
    }
}