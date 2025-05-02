using Common.Data;
using Common.Logic;
using Shared.Tp;
using Shared.Tp.Data;
using Shared.Tp.Ext.Hand;
using Shared.Tp.St.Sync;

namespace Server.Logic;

public sealed class ServerPeer : IDisposable, ISyncHandler<ServerState, ClientState>
{
    private readonly ServerSession _session;
    private readonly StSync<ServerState, ClientState> _stSync;

    public ITpReceiver Receiver => _stSync.Receiver;

    private readonly string _peerStateId;
    
    public ServerPeer(ServerSession session, ITpLink link)
    {
        _session = session;
        _stSync = StSyncFactory.CreateConnected(this, link);

        var handLink = link.Find<HandLink<ClientConnectState>>() ?? throw new("HandLink not found");
        _peerStateId = handLink.RemoteState?.PeerId ?? throw new("PeerId not found");
        _peerStateId = $"{_peerStateId}/{link.GetRemotePeerId()}";
    }

    public void Dispose()
    {
        _stSync.Dispose();
    }

    public bool PeerStateExists => _stSync.RemoteHistory.Count > 0; //TODO: remove adding first state to ClientConnectState
    public PeerState GetPeerState()
        => new()
        {
            Id = _peerStateId,
            ClientState = _stSync.RemoteStateRef
        };

    public void Update(float deltaTime)
        => _stSync.LocalUpdate(deltaTime);

    SyncOptions ISyncHandler<ServerState, ClientState>.Options => _session.SyncOptions;
    IObjWriter<StCmd<ServerState>> ISyncHandler<ServerState, ClientState>.LocalWriter { get; } 
        = TickStateFactory.CreateObjWriter<StCmd<ServerState>>();
    IObjReader<StCmd<ClientState>> ISyncHandler<ServerState, ClientState>.RemoteReader { get; } 
        = TickStateFactory.CreateObjReader<StCmd<ClientState>>();

    int ISyncHandler<ServerState, ClientState>.TimeMs => _session.TimeMs;
    ServerState ISyncHandler<ServerState, ClientState>.MakeLocalState()
        => _session.GetServerState();

    void ISyncHandler<ServerState, ClientState>.RemoteUpdated() { }

    void ISyncHandler<ServerState, ClientState>.RemoteDisconnected()
        => _session.PeerDisconnected(this);
}