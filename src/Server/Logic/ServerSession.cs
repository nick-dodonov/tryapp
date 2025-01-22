using System.Collections.Concurrent;
using Common.Logic;
using Cysharp.Threading;
using Server.Logic.Virtual;
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

    /// <summary>
    /// Session updates are switched on/off depending on connected at least one peer.
    /// It saves CPU on stage server now (don't want to waste it on idle running server).
    /// about looper: https://github.com/Cysharp/LogicLooper?tab=readme-ov-file#usage
    /// TODO: mv on/off to DynamicLooper (it will also allows to change target frame rate on the fly)
    /// </summary>
    private ILogicLooper? _logicLooper;
    private Task? _looperShutdownTask;

    private int _peerCount;
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
        return Task.CompletedTask;
    }

    async Task IHostedService.StopAsync(CancellationToken cancellationToken)
    {
        StopUpdates();
        var lastShutdownTask = _looperShutdownTask;
        if (lastShutdownTask != null)
        {
            _logger.Info("awaiting last looper shutdown");
            await lastShutdownTask;
        }
    }

    ITpReceiver ITpListener.Connected(ITpLink link)
    {
        _logger.Info($"{link}");
        var peer = new ServerPeer(
            new IdLogger(loggerFactory.CreateLogger<ServerPeer>(), link.GetRemotePeerId()),
            this, link);
        _peers.TryAdd(link, peer);
        if (Interlocked.Increment(ref _peerCount) == 1)
            StartUpdates();
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
            if (Interlocked.Decrement(ref _peerCount) == 0)
                StopUpdates();
        }
        else
            _logger.Warn($"peer not found: {link}");
    }

    private async void StartUpdates()
    {
        try
        {
            var previousShutdownTask = _looperShutdownTask;
            if (previousShutdownTask != null)
            {
                _logger.Info("awaiting previous looper shutdown");
                await previousShutdownTask;
            }

            //TODO: appsettings.json option (or possibly control from client for now)
            const int targetFrameRate = 10;
            _logger.Info($"starting loop: targetFrameRate={targetFrameRate}");
            _logicLooper = new LogicLooper(targetFrameRate);
            _ = _logicLooper.RegisterActionAsync(Update);
        }
        catch (Exception e)
        {
            _logger.Error($"{e}");
        }
    }

    private async void StopUpdates()
    {
        try
        {
            var logicLooper = Interlocked.Exchange(ref _logicLooper, null);
            if (logicLooper == null)
                return;
            var previousShutdownTask = _looperShutdownTask;
            if (previousShutdownTask != null)
            {
                _logger.Info("awaiting previous looper shutdown");
                await previousShutdownTask;
            }
            _logger.Info($"actions in loop: {logicLooper.ApproximatelyRunningActions}");
            _looperShutdownTask = logicLooper.ShutdownAsync(TimeSpan.FromSeconds(1));
            await _looperShutdownTask;
            _looperShutdownTask = null;
            var remainedActions = logicLooper.ApproximatelyRunningActions;
            if (remainedActions > 0)
                _logger.Warn($"still running actions in loop: {remainedActions}");
        }
        catch (Exception e)
        {
            _logger.Error($"{e}");
        }
    }

    private bool Update(in LogicLooperActionContext ctx)
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