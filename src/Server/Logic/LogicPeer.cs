using Shared.Session;
using Shared.Tp;
using Shared.Web;

namespace Server.Logic;

public class LogicPeer : IDisposable
{
    private readonly ITpLink _link;
    private readonly System.Timers.Timer _timer;
    private int _frameId;

    public ClientState LastClientState { get; set; }

    public LogicPeer(LogicSession session, ITpLink link)
    {
        _link = link;
        _timer = new(1000);
        _timer.Elapsed += (_, _) =>
        {
            var utcMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            //var msg = $"{frameId};TODO-FROM-SERVER;{utcMs}";

            var peerStates = session.GetPeerStates();
            var serverStateMsg = new ServerState
            {
                Frame = _frameId,
                UtcMs = utcMs,
                Peers = peerStates
            };
            _frameId++;

            var msg = WebSerializer.SerializeObject(serverStateMsg);
            var bytes = System.Text.Encoding.UTF8.GetBytes(msg);
            link.Send(bytes);
        };
        _timer.Start();
    }

    public void Dispose()
    {
        _timer.Dispose();
        _link.Dispose();
    }
}