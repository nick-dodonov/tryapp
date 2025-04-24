using Common.Logic;
using Common.Logic.Shared;
using Shared.Tp;
using Shared.Tp.Ext.Hand;

namespace Server.Logic;

public sealed class ServerPeer : IDisposable, ISyncHandler<ServerState, ClientState>
{
    private readonly ServerSession _session;
    private readonly StateSyncer<ServerState, ClientState> _stateSyncer;

    public ITpReceiver Receiver => _stateSyncer.Receiver;

    private readonly string _peerStateId;
    
    public ServerPeer(ServerSession session, ITpLink link)
    {
        _session = session;
        _stateSyncer = StateSyncerFactory.CreateConnected(this, link);

        var handLink = link.Find<HandLink<ClientConnectState>>() ?? throw new("HandLink not found");
        _peerStateId = handLink.RemoteState?.PeerId ?? throw new("PeerId not found");
        _peerStateId = $"{_peerStateId}/{link.GetRemotePeerId()}";
    }

    public void Dispose()
    {
        _stateSyncer.Dispose();
    }

    public PeerState GetPeerState()
        => new()
        {
            Id = _peerStateId,
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