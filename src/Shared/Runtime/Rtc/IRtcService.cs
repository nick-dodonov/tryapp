using System.Threading;
using System.Threading.Tasks;

namespace Shared.Rtc
{
    /// <summary>
    /// TODO: make different implementations (not only current REST variant but WebSocket too)
    /// </summary>
    public interface IRtcService
    {
        //TODO: shared RTC types for SDP (offer, answer) and ICE candidates
        public ValueTask<string> GetOffer(string id, CancellationToken cancellationToken);
        public ValueTask<string> SetAnswer(string id, string answer, CancellationToken cancellationToken);
        public ValueTask AddIceCandidates(string id, string candidates, CancellationToken cancellationToken);
    }
}