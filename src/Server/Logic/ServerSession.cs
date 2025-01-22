using System.Collections.Concurrent;
using Common.Logic;
using Cysharp.Threading;
using Server.Logic.Virtual;
using Shared.Log;
using Shared.Log.Asp;
using Shared.Tp;
using Shared.Tp.Ext.Misc;

namespace Server.Logic;

public class ServerSession(ILoggerFactory loggerFactory, ITpApi tpApi, ILogicLooper logicLooper)
    : IHostedService, ITpListener, ITpReceiver
{
    private readonly ILogger _logger = loggerFactory.CreateLogger<ServerSession>();

    private readonly TimeLink.Api _timeApi = tpApi.Find<TimeLink.Api>() ?? throw new("TimeLink.Api not found");
    private readonly ConcurrentDictionary<ITpLink, ServerPeer> _peers = new();

    private readonly IVirtualPeer[] _virtualPeers =
    [
        new CircleVirtualPeer("VirtualC0", 0x8F7F7F, 0.0f, 0.8f, 60_000, 1),
        //new CircleVirtualPeer("VirtualC1", 0x9F7F7F, 0.5f, 0.6f, 20_000, -1),
        new LinearVirtualPeer("VirtualL0", 0x7F7F8F, new(0.5f, 0.6f), new(0.1f, -0.1f)),
    ];

    Task IHostedService.StartAsync(CancellationToken cancellationToken)
    {
        _logger.Info("start listening");
        tpApi.Listen(this);

        // https://github.com/Cysharp/LogicLooper/blob/master/samples/LoopHostingApp/LoopHostedService.cs
        _ = logicLooper.RegisterActionAsync(Update, cancellationToken);
        return Task.CompletedTask;
    }

    async Task IHostedService.StopAsync(CancellationToken cancellationToken)
    {
        await logicLooper.ShutdownAsync(TimeSpan.FromSeconds(5));
        var remainedActions = logicLooper.ApproximatelyRunningActions;
        if (remainedActions > 0)
            _logger.Warn($"{remainedActions} actions are remained in loop");
    }

    private bool Update(in LogicLooperActionContext ctx, CancellationToken state)
    {
        if (ctx.CancellationToken.IsCancellationRequested)
        {
            _logger.Info("shutdown");
            return false;
        }

        //_logger.Info($"{ctx.CurrentFrame}");
        foreach (var virtualPeer in _virtualPeers)
            virtualPeer.Update((float)ctx.ElapsedTimeFromPreviousFrame.TotalSeconds);
        return true;
    }

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
            .Concat(_virtualPeers.Select(x => x.GetPeerState(frame, sessionMs)))
            .ToArray();
        return new()
        {
            Frame = frame,
            Ms = sessionMs,
            Peers = peerStates
        };
    }
}