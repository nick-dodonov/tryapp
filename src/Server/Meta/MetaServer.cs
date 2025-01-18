using Shared.Log;
using Shared.Meta.Api;
using Shared.Tp.Rtc;
using Shared.Web;

namespace Server.Meta;

public sealed class MetaServer(
    IRtcService rtcService, 
    ILogger<MetaServer> logger) 
    : IMeta
{
    private static readonly string[] RandomNames =
    [
        "Tokyo", "Delhi", "Paris", "Washington", "Ottawa", "Berlin", "Beijing", "Canberra", "London", "Moscow",
        "Brasília", "Madrid", "Rome", "Seoul", "Bangkok", "Jakarta", "Cairo", "Riyadh", "Tehran", "Mexico City",
        "Pretoria", "Buenos Aires", "Athens", "Kabul", "Hanoi", "Baghdad", "Damascus", "Ankara", "Helsinki", "Oslo",
        "Stockholm", "Copenhagen", "Wellington", "Luxembourg", "Brussels", "Lisbon", "Dublin", "Warsaw", "Prague", 
        "Vienna", "Zagreb", "Sofia", "Bucharest", "Belgrade", "Bern", "Reykjavik", "Montevideo", "Doha", "Amman", 
        "Singapore", "Osaka", "Manila", "Toronto", "Lima", "Cape Town", "Taipei", "Istanbul"
    ];
    private int _uid;

    public void Dispose() { }

    IRtcService IMeta.RtcService => rtcService;

    ValueTask<ServerInfo> IMeta.GetInfo(CancellationToken cancellationToken)
    {
        var result = new ServerInfo
        {
            RequestId = ++_uid,
            RequestTime = DateTime.Now,
            RandomName = RandomNames[new Random().Next(RandomNames.Length)],
        };

        logger.Info(WebSerializer.SerializeObject(result));
        return new(result);
    }
}