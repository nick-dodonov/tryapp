using System.Threading;
using System.Threading.Tasks;
using Shared.Log;

namespace Shared.Tp.Rtc
{
    /// <summary>
    /// Common helper for different WebRTC implementations working with signalling service
    /// </summary>
    public abstract class BaseRtcLink : ITpLink
    {
        private readonly Slog.Area _log = new();

        private readonly IRtcService _service;
        private readonly ITpReceiver _receiver;

        private string? _linkId; // null until offer is not obtained
        
        public abstract void Dispose();
        public abstract string GetRemotePeerId();
        public abstract void Send(byte[] bytes);

        protected BaseRtcLink(IRtcService service, ITpReceiver receiver)
        {
            _service = service;
            _receiver = receiver;
        }

        protected async Task<string> ObtainOffer(CancellationToken cancellationToken)
        {
            _log.Info("request");
            var offer = await _service.GetOffer(cancellationToken);
            _log.Info($"result: {offer}");

            _linkId = offer.LinkId;
            _log.AddCategorySuffix($"-{_linkId}");

            return offer.SdpInitJson;
        }

        protected async Task<string> ReportAnswer(string answerJson, CancellationToken cancellationToken)
        {
            _log.Info($"request: {answerJson}");
            var candidatesListJson = await _service.SetAnswer(_linkId!, answerJson, cancellationToken);
            _log.Info($"result: {candidatesListJson}");
            return candidatesListJson;
        }

        protected async Task ReportIceCandidates(string candidatesJson, CancellationToken cancellationToken)
        {
            _log.Info($"request: {candidatesJson}");
            await _service.AddIceCandidates(_linkId!, candidatesJson, cancellationToken);
            _log.Info("complete");
        }

        protected void CallReceived(byte[]? bytes)
        {
            if (bytes == null)
                _log.Info("disconnected");

            _receiver.Received(this, bytes);
        }
    }
}