using Shared;
using Shared.Meta.Api;

namespace Server.Meta;

public class MetaServer : IMeta
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

    public ValueTask<ServerInfo> GetInfo(CancellationToken cancellationToken)
    {
        var result = new ServerInfo
        {
            RandomName = RandomNames[new Random().Next(RandomNames.Length)],
            RequestId = ++_uid,
            RequestTime = DateTime.Now
        };

        StaticLog.Info($"==== Info request/result: {result.RandomName} {result.RequestId} {result.RequestTime}");
        return new(result);
    }
}