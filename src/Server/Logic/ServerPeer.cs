using Common.Logic;
using Shared.Log.Asp;
using Shared.Tp;

namespace Server.Logic;

public sealed class ServerPeer : IDisposable, ISyncHandler<ServerState, ClientState>
{
    //private readonly ILogger _logger;
    private readonly ServerSession _session;
    private readonly ITpLink _link;

    private readonly StateSyncer<ServerState, ClientState> _stateSyncer;

    public ServerPeer(ServerSession session, ITpLink link, ILoggerFactory loggerFactory)
    {
        //_logger = new IdLogger(loggerFactory.CreateLogger<ServerPeer>(), link.GetRemotePeerId());
        _session = session;
        _link = link;

        var syncerLogger = new IdLogger(
            loggerFactory.CreateLogger<StateSyncer<ServerState, ClientState>>(),
            link.GetRemotePeerId());
        _stateSyncer = new(this, _link, syncerLogger);
    }

    public void Dispose()
    {
        _stateSyncer.Dispose();
        _link.Dispose();
    }

    public PeerState GetPeerState() =>
        new()
        {
            Id = _link.GetRemotePeerId(),
            ClientState = _stateSyncer.RemoteState
        };

    public void Update(float deltaTime) => 
        _stateSyncer.LocalUpdate(deltaTime);

    public void Received(ReadOnlySpan<byte> span) => 
        _stateSyncer.RemoteUpdate(span);

    SyncOptions ISyncHandler<ServerState, ClientState>.Options => _session.SyncOptions;

    ServerState ISyncHandler<ServerState, ClientState>.MakeLocalState(int sendIndex) => 
        _session.GetServerState(sendIndex);

    void ISyncHandler<ServerState, ClientState>.ReceivedRemoteState(ClientState remoteState) { }
}