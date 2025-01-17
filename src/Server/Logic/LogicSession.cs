using System.Collections.Concurrent;
using System.Text;
using Shared.Log;
using Shared.Session;
using Shared.Tp;
using Shared.Web;

namespace Server.Logic;

public class LogicSession(ILogger<LogicSession> logger, ITpApi tpApi) 
    : IHostedService, ITpListener, ITpReceiver
{
    private readonly ConcurrentDictionary<ITpLink, LogicPeer> _peers = new();
    
    Task IHostedService.StartAsync(CancellationToken cancellationToken)
    {
        tpApi.Listen(this);
        return Task.CompletedTask;
    }

    Task IHostedService.StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    ITpReceiver ITpListener.Connected(ITpLink link)
    {
        logger.Info($"{link}");
        var peer = new LogicPeer(this, link);
        _peers.TryAdd(link, peer);
        return this;
    }

    void ITpReceiver.Received(ITpLink link, byte[]? bytes)
    {
        if (bytes == null)
        {
            logger.Info($"disconnected: {link}");
            if (!_peers.TryRemove(link, out var peer))
                logger.Warn($"peer not connected: {link}");
            else
                peer.Dispose();
        }
        else
        {
            var content = Encoding.UTF8.GetString(bytes);
            logger.Info($"[{bytes.Length}]: {content}");
            if (!_peers.TryGetValue(link, out var peer))
                logger.Warn($"peer not connected: {link}");
            else
            {
                try
                {
                    peer.LastClientState = WebSerializer.DeserializeObject<ClientState>(content);
                }
                catch (Exception e)
                {
                    logger.Error($"failed to deserialize: {e}");
                }
            }
        }
    }

    public ClientState[] CollectClientStates()
        => _peers.Select(x => x.Value.LastClientState).ToArray();
}
