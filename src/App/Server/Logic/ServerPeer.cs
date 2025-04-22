using Common.Logic;
using Common.Logic.Shared;
using Shared.Tp;

namespace Server.Logic;

public sealed class ServerPeer : IDisposable, ISyncHandler<ServerState, ClientState>
{
    private readonly ServerSession _session;
    private readonly StateSyncer<ServerState, ClientState> _stateSyncer;

    private readonly StdCmdLink<ServerState, ClientState> _cmdLink;
    public ITpReceiver Receiver => _cmdLink;

    public ServerPeer(ServerSession session, ITpLink link)
    {
        _session = session;
        _stateSyncer = new(this);
        _cmdLink = new(link, _stateSyncer);
        _stateSyncer.Init(_cmdLink);
    }

    public void Dispose()
    {
        _stateSyncer.Dispose();
        _cmdLink.Dispose();
    }

    public PeerState GetPeerState()
        => new()
        {
            Id = _cmdLink.Link.GetRemotePeerId(),
            ClientState = _stateSyncer.RemoteState
        };

    public void Update(float deltaTime)
        => _stateSyncer.LocalUpdate(deltaTime);

    SyncOptions ISyncHandler<ServerState, ClientState>.Options => _session.SyncOptions;

    ServerState ISyncHandler<ServerState, ClientState>.MakeLocalState(int sendIndex)
        => _session.GetServerState(sendIndex);

    void ISyncHandler<ServerState, ClientState>.RemoteUpdated(ClientState remoteState) { }

    void ISyncHandler<ServerState, ClientState>.RemoteDisconnected()
        => _session.PeerDisconnected(this);
}