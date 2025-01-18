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
        private readonly Slog.Area _log;

        private readonly IRtcService _service;
        private readonly ITpReceiver _receiver;

        private int _linkId = -1; // -1 until offer is not obtained
        private string? _linkToken; // null until offer is not obtained
        
        public abstract void Dispose();
        public abstract string GetRemotePeerId();
        public abstract void Send(byte[] bytes);

        protected BaseRtcLink(IRtcService service, ITpReceiver receiver)
        {
            _log = new(GetType().Name);
            
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
            _linkToken = offer.LinkToken;

            return offer.SdpInit.Json;
        }

        protected async Task<RtcIceCandidate[]> ReportAnswer(RtcSdpInit answer, CancellationToken cancellationToken)
        {
            _log.Info($"request: {answer}");
            var candidates = await _service.SetAnswer(_linkToken!, answer, cancellationToken);
            _log.Info($"result: {candidates}");
            return candidates;
        }

        protected async Task ReportIceCandidates(RtcIceCandidate[] candidates, CancellationToken cancellationToken)
        {
            _log.Info($"request: {candidates}");
            await _service.AddIceCandidates(_linkToken!, candidates, cancellationToken);
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