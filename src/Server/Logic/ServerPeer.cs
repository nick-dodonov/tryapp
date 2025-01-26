using Common.Logic;
using Shared.Log;
using Shared.Tp;

namespace Server.Logic;

public sealed class ServerPeer : IDisposable, ISyncHandler<ServerState>
{
    private readonly ILogger _logger;
    private readonly ServerSession _session;
    private readonly ITpLink _link;

    private readonly StateSyncer<ServerState, ClientState> _stateSyncer;

    public ServerPeer(ServerSession session, ITpLink link, ILogger logger)
    {
        _logger = logger;
        _session = session;
        _link = link;

        _stateSyncer = new(this, _link);
    }

    public void Dispose()
    {
        _stateSyncer.Dispose();
        _link.Dispose();
    }

    public void Received(ReadOnlySpan<byte> span)
    {
        try
        {
            _stateSyncer.RemoteUpdate(span);
        }
        catch (Exception e)
        {
            _logger.Error($"{e}");
        }
    }

    public PeerState GetPeerState() =>
        new()
        {
            Id = _link.GetRemotePeerId(),
            ClientState = _stateSyncer.RemoteState
        };

    public void Update(float deltaTime)
    {
        _stateSyncer.LocalUpdate(deltaTime);
    }

    SyncOptions ISyncHandler<ServerState>.Options => _session.SyncOptions;

    ServerState ISyncHandler<ServerState>.MakeLocalState(int sendIndex) => 
        _session.GetServerState(sendIndex);
}