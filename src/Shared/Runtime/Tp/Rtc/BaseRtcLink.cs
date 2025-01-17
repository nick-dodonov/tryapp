using System;
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

        private readonly string _clientId; //TODO: client id can be obtained from offer instead of generation
        private readonly IRtcService _service;
        private readonly ITpReceiver _receiver;

        public abstract void Dispose();
        public abstract string GetRemotePeerId();
        public abstract void Send(byte[] bytes);

        protected BaseRtcLink(string localPeerId, IRtcService service, ITpReceiver receiver)
        {
            _clientId = localPeerId;
            _service = service;
            _receiver = receiver;

            _log = new($"{nameof(BaseRtcLink)}[{_clientId}]");
        }

        protected async Task<string> ObtainOffer(CancellationToken cancellationToken)
        {
            _log.Info("request");
            var offerStr = await _service.GetOffer(_clientId, cancellationToken);
            _log.Info($"result: {offerStr}");
            return offerStr;
        }

        protected async Task<string> ReportAnswer(string answerJson, CancellationToken cancellationToken)
        {
            _log.Info($"request: {answerJson}");
            var candidatesListJson = await _service.SetAnswer(_clientId, answerJson, cancellationToken);
            _log.Info($"result: {candidatesListJson}");
            return candidatesListJson;
        }

        protected async Task ReportIceCandidates(string candidatesJson, CancellationToken cancellationToken)
        {
            _log.Info($"request: {candidatesJson}");
            await _service.AddIceCandidates(_clientId, candidatesJson, cancellationToken);
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