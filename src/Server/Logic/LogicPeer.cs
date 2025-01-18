using System.Text;
using Common.Logic;
using Shared.Log;
using Shared.Tp;
using Shared.Web;

namespace Server.Logic;

public sealed class LogicPeer : IDisposable
{
    private readonly ILogger _logger;
    private readonly LogicSession _session;
    private readonly ITpLink _link;

    private readonly System.Timers.Timer _timer;

    private ClientState _lastClientState;

    public LogicPeer(ILogger logger, LogicSession session, ITpLink link)
    {
        _logger = logger;
        _session = session;
        _link = link;
        _timer = new(1000);

        var frame = 0;
        _timer.Elapsed += (_, _) => { Send(frame++); };
        _timer.Start();
    }

    public void Dispose()
    {
        _timer.Dispose();
        _link.Dispose();
    }

    private void Send(int frame)
    {
        var serverState = _session.GetServerState(frame);

        var msg = WebSerializer.SerializeObject(serverState);
        var bytes = Encoding.UTF8.GetBytes(msg);

        _logger.Info($"[{bytes.Length}] bytes: {msg}");
        _link.Send(bytes);
    }

    public void Received(byte[] bytes)
    {
        try
        {
            var msg = Encoding.UTF8.GetString(bytes);
            _logger.Info($"[{bytes.Length}] bytes: {msg}");
            _lastClientState = WebSerializer.DeserializeObject<ClientState>(msg);
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
}