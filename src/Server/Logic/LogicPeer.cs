using Shared.Rtc;
using Shared.Session;
using Shared.Web;

namespace Server.Logic;

public class LogicPeer : IDisposable
{
    private readonly IRtcLink _link;
    private readonly System.Timers.Timer _timer;
    private int _frameId;

    public ClientState LastClientState { get; set; }

    public LogicPeer(LogicSession session, IRtcLink link)
    {
        _link = link;
        _timer = new(1000);
        _timer.Elapsed += (_, _) =>
        {
            var utcMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            //var msg = $"{frameId};TODO-FROM-SERVER;{utcMs}";

            var clientStates = session.CollectClientStates();
            var peerStates = clientStates.Select(clientState => new PeerState
            {
                Id = "<unknown>",
                ClientState = clientState
            }).ToArray();
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