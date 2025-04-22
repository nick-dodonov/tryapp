using Common.Logic;
using Shared.Tp;

namespace Server.Logic;

public sealed class ServerPeer : IDisposable, ICmdReceiver<ClientState>, ISyncHandler<ServerState, ClientState>
{
    //private readonly ILogger _logger;
    private readonly ServerSession _session;

    private readonly StdCmdLink<ServerState, ClientState> _cmdLink;

    public ITpReceiver Receiver => _cmdLink;
    public ITpLink Link => _cmdLink.Link;

    private readonly StateSyncer<ServerState, ClientState> _stateSyncer;

    public ServerPeer(ServerSession session, ITpLink link, ILoggerFactory loggerFactory)
    {
        //_logger = new IdLogger(loggerFactory.CreateLogger<ServerPeer>(), link.GetRemotePeerId());
        _session = session;
        _cmdLink = new(link, this);

        // var syncerLogger = new IdLogger(
        //     loggerFactory.CreateLogger<StateSyncer<ServerState, ClientState>>(),
        //     link.GetRemotePeerId());
        _stateSyncer = new(this, _cmdLink);
    }

    public void Dispose()
    {
        _stateSyncer.Dispose();
        _cmdLink.Dispose();
    }

    public PeerState GetPeerState() 
        => new()
        {
            Id = Link.GetRemotePeerId(),
            ClientState = _stateSyncer.RemoteState
        };

    public void Update(float deltaTime)
        => _stateSyncer.LocalUpdate(deltaTime);

    void ICmdReceiver<ClientState>.CmdReceived(in ClientState cmd) 
        => _stateSyncer.RemoteUpdate(cmd);

    void ICmdReceiver<ClientState>.CmdDisconnected() 
        => _session.PeerDisconnected(this);

    SyncOptions ISyncHandler<ServerState, ClientState>.Options => _session.SyncOptions;

    ServerState ISyncHandler<ServerState, ClientState>.MakeLocalState(int sendIndex) 
        => _session.GetServerState(sendIndex);

    void ISyncHandler<ServerState, ClientState>.ReceivedRemoteState(ClientState remoteState) { }
}