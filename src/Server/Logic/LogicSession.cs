using System.Text;
using Shared.Log;
using Shared.Rtc;

namespace Server.Logic;

public class LogicSession(ILogger<LogicSession> logger, IRtcApi rtcApi) 
    : IHostedService
{
    Task IHostedService.StartAsync(CancellationToken cancellationToken)
    {
        rtcApi.Listen(Connected);
        return Task.CompletedTask;
    }

    Task IHostedService.StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private IRtcLink.ReceivedCallback Connected(IRtcLink link)
    {
        logger.Info($"{link}");
        return Received;
    }

    private void Received(IRtcLink link, byte[]? bytes)
    {
        if (bytes == null)
            logger.Info("disconnected");
        else
        {
            var content = Encoding.UTF8.GetString(bytes);
            logger.Info($"[{bytes.Length}]: {content}");
        }
    }
}
