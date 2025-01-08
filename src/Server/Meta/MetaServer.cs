using Shared.Log;
using Shared.Meta.Api;
using Shared.Rtc;

namespace Server.Meta;

public sealed class MetaServer(IRtcService rtcService, ILogger<MetaServer> logger) : IMeta
{
    private static readonly string[] RandomNames =
    [
        "Tokyo", "Delhi", "Paris", "Washington", "Ottawa", "Berlin", "Beijing", "Canberra", "London", "Moscow",
        "Brasília", "Madrid", "Rome", "Seoul", "Bangkok", "Jakarta", "Cairo", "Riyadh", "Tehran", "Mexico City",
        "Pretoria", "Buenos Aires", "Athens", "Kabul", "Hanoi", "Baghdad", "Damascus", "Ankara", "Helsinki", "Oslo",
        "Stockholm", "Copenhagen", "Wellington", "Luxembourg", "Brussels", "Lisbon", "Dublin", "Warsaw", "Prague", "Vienna",
        "Zagreb", "Sofia", "Bucharest", "Belgrade", "Bern", "Reykjavik", "Montevideo", "Doha", "Amman", "Singapore"
    ];
    private int _uid;

    public void Dispose() { }

    public ValueTask<ServerInfo> GetInfo(CancellationToken cancellationToken)
    {
        var result = new ServerInfo
        {
            RandomName = RandomNames[new Random().Next(RandomNames.Length)],
            RequestId = ++_uid,
            RequestTime = DateTime.Now
        };

        logger.Info($"{result.RandomName} {result.RequestId} {result.RequestTime}");
        return new(result);
    }

    public ValueTask<string> GetOffer(string id, CancellationToken cancellationToken) 
        => rtcService.GetOffer(id, cancellationToken);

    public ValueTask<string> SetAnswer(string id, string answerJson, CancellationToken cancellationToken)
        => rtcService.SetAnswer(id, answerJson, cancellationToken);

    public ValueTask AddIceCandidates(string id, string candidates, CancellationToken cancellationToken)
        => rtcService.AddIceCandidates(id, candidates, cancellationToken);
}