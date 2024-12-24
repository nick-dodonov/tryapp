using Server.Rtc;
using Shared;
using Shared.Meta.Api;
using SIPSorcery.Net;

namespace Server.Meta;

public sealed class MetaServer(RtcService rtcService) : IMeta
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

        StaticLog.Info($"==== Info request/result: {result.RandomName} {result.RequestId} {result.RequestTime}");
        return new(result);
    }

    public async ValueTask<string> GetOffer(string id, CancellationToken cancellationToken) 
        => (await rtcService.GetOffer(id)).toJSON();

    public ValueTask<string> SetAnswer(string id, string answerJson, CancellationToken cancellationToken)
    {
        if (!RTCSessionDescriptionInit.TryParse(answerJson, out var answer))
            throw new ApplicationException("Body must contain SDP answer for id: {id}");
        return rtcService.SetAnswer(id, answer, cancellationToken);
    }
}