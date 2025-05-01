using System.Collections.Concurrent;
using Common.Data;
using Cysharp.Threading;
using Microsoft.Extensions.Options;
using Server.Logic.Virtual;
using Shared.Log;
using Shared.Tp;
using Shared.Tp.Ext.Misc;
using Shared.Tp.St.Sync;

namespace Server.Logic;

public sealed class ServerSession : IDisposable, IHostedService, ITpListener
{
    private readonly ILogger _logger;

    private readonly ITpApi _tpApi;
    private readonly TimeLink.Api _timeApi;

    private SyncOptions _syncOptions;
    private readonly IDisposable? _syncOptionsDisposable;
    public SyncOptions SyncOptions => _syncOptions;

    /// <summary>
    /// Session updates are switched on/off depending on connected at least one peer.
    /// It saves CPU on stage server now (don't want to waste it on idle running server).
    /// about looper: https://github.com/Cysharp/LogicLooper?tab=readme-ov-file#usage
    /// TODO: mv on/off to DynamicLooper (it will also allows to change target frame rate on the fly)
    /// </summary>
    private ILogicLooper? _logicLooper;
    private Task? _looperShutdownTask;

    private int _peerCount;
    private readonly ConcurrentDictionary<ServerPeer, ITpLink> _peers = new();

    private readonly IVirtualPeer[] _virtualPeers =
    [
        new CircleVirtualPeer("VirtualC0", 0x8F7F7F, 0.0f, 0.8f, 60_000, 1),
        //new CircleVirtualPeer("VirtualC1", 0x9F7F7F, 0.5f, 0.6f, 20_000, -1),
        new LinearVirtualPeer("VirtualL0", 0x7F7F8F, new(0.5f, 0.6f), new(0.2f, -0.2f)),
    ];

    public ServerSession(
        IOptionsMonitor<SyncOptions> syncOptionsMonitor, 
        ITpApi tpApi, 
        ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<ServerSession>();

        _tpApi = tpApi;
        _timeApi = tpApi.Find<TimeLink.Api>() ?? throw new("TimeLink.Api not found");
        
        _syncOptions = syncOptionsMonitor.CurrentValue;
        _syncOptionsDisposable = syncOptionsMonitor.OnChange((o, _) => _syncOptions = o);
    }

    void IDisposable.Dispose()
    {
        _syncOptionsDisposable?.Dispose();
    }
    
    Task IHostedService.StartAsync(CancellationToken cancellationToken)
    {
        _logger.Info("start listening");
        _tpApi.Listen(this);
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
        _logger.Info($"peer connecting: {link}");
        var peer = new ServerPeer(this, link);
        _peers.TryAdd(peer, link);
        if (Interlocked.Increment(ref _peerCount) == 1)
            StartUpdates();
        return peer.Receiver;
    }

    public void PeerDisconnected(ServerPeer peer)
    {
        if (_peers.TryRemove(peer, out var link))
        {
            _logger.Info($"peer disconnected: {link}");
            peer.Dispose();
            if (Interlocked.Decrement(ref _peerCount) == 0)
                StopUpdates();
        }
        else
            _logger.Warn($"peer not found: {peer}");
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

        var deltaTime = (float)ctx.ElapsedTimeFromPreviousFrame.TotalSeconds;
        //_logger.Info($"{ctx.CurrentFrame} - {deltaTime}");

        //TODO: share virtual/server peer interfaces
        foreach (var peer in _peers.Keys)
            peer.Update(deltaTime);
        foreach (var virtualPeer in _virtualPeers)
            virtualPeer.Update(deltaTime);

        return true;
    }
    
    public ServerState GetServerState()
    {
        var sessionMs = _timeApi.LocalMs;
        var peerStates = _peers
            .Select(x => x.Key.GetPeerState())
            .Concat(_virtualPeers.Select(x => x.GetPeerState(sessionMs)))
            .ToArray();
        return new()
        {
            Ms = sessionMs,
            Peers = peerStates
        };
    }
}