using System.Collections.Concurrent;
using System.Text;
using Common.Logic;
using Shared.Log;
using Shared.Tp;
using Shared.Web;

namespace Server.Logic;

public class LogicSession(ILoggerFactory loggerFactory, ITpApi tpApi) 
    : IHostedService, ITpListener, ITpReceiver
{
    private readonly ConcurrentDictionary<ITpLink, LogicPeer> _peers = new();
    private readonly ILogger _logger = loggerFactory.CreateLogger<LogicSession>();
    
    Task IHostedService.StartAsync(CancellationToken cancellationToken)
    {
        tpApi.Listen(this);
        return Task.CompletedTask;
    }

    Task IHostedService.StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    ITpReceiver ITpListener.Connected(ITpLink link)
    {
        _logger.Info($"{link}");
        var peer = new LogicPeer(loggerFactory.CreateLogger<LogicPeer>(), this, link);
        _peers.TryAdd(link, peer);
        return this;
    }

    void ITpReceiver.Received(ITpLink link, byte[]? bytes)
    {
        if (bytes == null)
        {
            _logger.Info($"disconnected: {link}");
            if (!_peers.TryRemove(link, out var peer))
                _logger.Warn($"peer not connected: {link}");
            else
                peer.Dispose();
        }
        else
        {
            var content = Encoding.UTF8.GetString(bytes);
            _logger.Info($"[{bytes.Length}]: {content}");
            if (!_peers.TryGetValue(link, out var peer))
                _logger.Warn($"peer not connected: {link}");
            else
            {
                try
                {
                    peer.LastClientState = WebSerializer.DeserializeObject<ClientState>(content);
                }
                catch (Exception e)
                {
                    _logger.Error($"failed to deserialize: {e}");
                }
            }
        }
    }

    public PeerState[] GetPeerStates() =>
        _peers.Select(x => new PeerState
        {
            Id = x.Key.GetRemotePeerId(),
            ClientState = x.Value.LastClientState
        }).ToArray();
}
