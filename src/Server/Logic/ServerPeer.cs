using Common.Logic;
using Shared.Log;
using Shared.Tp;
using Shared.Web;

namespace Server.Logic;

public sealed class ServerPeer : IDisposable, ISyncHandler<ServerState>
{
    private readonly ILogger _logger;
    private readonly ServerSession _session;
    private readonly ITpLink _link;

    private readonly StateSyncer<ServerState> _serverStateSyncer;
    
    private ClientState _lastClientState;

    public ServerPeer(ILogger logger, ServerSession session, ITpLink link)
    {
        _logger = logger;
        _session = session;
        _link = link;

        _serverStateSyncer = new(
            new() { basicSendRate = 1 }, //TODO: options customize
            this,
            _link);
    }

    public void Dispose()
    {
        _serverStateSyncer.Dispose();
        _link.Dispose();
    }

    public void Received(ReadOnlySpan<byte> span)
    {
        try
        {
            _lastClientState = WebSerializer.Default.Deserialize<ClientState>(span);
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
            ClientState = _lastClientState
        };

    public void Update(float deltaTime)
    {
        _serverStateSyncer.LocalUpdate(deltaTime);
    }

    ServerState ISyncHandler<ServerState>.MakeState(int sendIndex) => 
        _session.GetServerState(sendIndex);
}