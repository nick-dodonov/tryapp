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

    public ClientState LastClientState { get; set; }

    public LogicPeer(ILogger logger, LogicSession session, ITpLink link)
    {
        _logger = logger;
        _session = session;
        _link = link;
        _timer = new(1000);

        var frame = 0;
        _timer.Elapsed += (_, _) =>
        {
            Send(frame++);
        };
        _timer.Start();
    }

    public void Dispose()
    {
        _timer.Dispose();
        _link.Dispose();
    }

    private void Send(int frame)
    {
        var utcMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var peerStates = _session.GetPeerStates();
        var serverStateMsg = new ServerState
        {
            Frame = frame,
            UtcMs = utcMs,
            Peers = peerStates
        };

        var msg = WebSerializer.SerializeObject(serverStateMsg);
        _logger.Info(msg);

        var bytes = System.Text.Encoding.UTF8.GetBytes(msg);
        _link.Send(bytes);
    }
}