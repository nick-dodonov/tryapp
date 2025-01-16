using System;
using System.Threading;
using System.Threading.Tasks;
using Shared.Log;
using Shared.Rtc;

namespace Client.Rtc
{
    /// <summary>
    /// Common helper for different WebRTC implementations working with signalling service
    /// </summary>
    public abstract class BaseRtcLink : IRtcLink
    {
        private readonly Slog.Area _log;
        
        private readonly string _clientId; //TODO: client id can be obtained from offer instead of generation
        private readonly IRtcService _service; 
        private readonly IRtcLink.ReceivedCallback _receivedCallback;
        private IRtcLink _rtcLinkImplementation;

        public abstract void Dispose();
        public abstract void Send(byte[] bytes);

        protected BaseRtcLink(IRtcService service, IRtcLink.ReceivedCallback receivedCallback)
        {
            _clientId = Guid.NewGuid().ToString();
            _service = service;
            _receivedCallback = receivedCallback;

            _log = new($"{nameof(BaseRtcLink)}[{_clientId}]");
        }

        protected async Task<string> ObtainOffer(CancellationToken cancellationToken)
        {
            _log.Info("request");
            var offerStr = await _service.GetOffer(_clientId, cancellationToken);
            _log.Info($"result: {offerStr}");
            return offerStr;
        }

        protected internal async Task<string> ReportAnswer(string answerJson, CancellationToken cancellationToken)
        {
            _log.Info($"request: {answerJson}");
            var candidatesListJson = await _service.SetAnswer(_clientId, answerJson, cancellationToken);
            _log.Info($"result: {candidatesListJson}");
            return candidatesListJson;
        }

        protected internal async Task ReportIceCandidates(string candidatesJson, CancellationToken cancellationToken)
        {
            _log.Info($"request: {candidatesJson}");
            await _service.AddIceCandidates(_clientId, candidatesJson, cancellationToken);
            _log.Info("complete");
        }

        protected internal void CallReceived(byte[] bytes)
        {
            if (bytes == null)
                _log.Info("disconnected");
            _receivedCallback(this, bytes);
        }
    }
}