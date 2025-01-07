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
    public class BaseRtcLink
    {
        private readonly string _clientId; //TODO: client id can be obtained from offer instead of generation
        private readonly IRtcService _service; 
        private readonly IRtcLink.ReceivedCallback _receivedCallback;

        protected BaseRtcLink(IRtcService service, IRtcLink.ReceivedCallback receivedCallback)
        {
            _clientId = Guid.NewGuid().ToString();
            _service = service;
            _receivedCallback = receivedCallback;
        }

        protected async Task<string> ObtainOffer(CancellationToken cancellationToken)
        {
            Slog.Info($"BaseRtcLink: ObtainOffer: request id={_clientId}");
            var offerStr = await _service.GetOffer(_clientId, cancellationToken);
            Slog.Info($"BaseRtcLink: ObtainOffer: result: id={_clientId}: {offerStr}");
            return offerStr;
        }

        protected internal async Task<string> ReportAnswer(string answerJson, CancellationToken cancellationToken)
        {
            Slog.Info($"BaseRtcLink: ReportAnswer: request id={_clientId}: {answerJson}");
            var candidatesListJson = await _service.SetAnswer(_clientId, answerJson, cancellationToken);
            Slog.Info($"BaseRtcLink: ReportAnswer: result id={_clientId}: {candidatesListJson}");
            return candidatesListJson;
        }

        protected internal async Task ReportIceCandidates(string candidatesJson, CancellationToken cancellationToken)
        {
            Slog.Info($"BaseRtcLink: ReportIceCandidates: request id={_clientId}: {candidatesJson}");
            await _service.AddIceCandidates(_clientId, candidatesJson, cancellationToken);
            Slog.Info($"BaseRtcLink: ReportIceCandidates: complete id={_clientId}");
        }

        protected internal void CallReceived(byte[] bytes)
        {
            // Slog.Info(bytes != null
            //     ? $"BaseRtcLink: CallReceived: id={_clientId}: {bytes.Length} bytes"
            //     : $"BaseRtcLink: CallReceived: id={_clientId}: disconnect");
            // var messageStr = System.Text.Encoding.UTF8.GetString(bytes);
            // Slog.Info($"BaseRtcLink: CallReceived: {messageStr}");
            if (bytes == null)
                Slog.Info($"BaseRtcLink: CallReceived: id={_clientId}: disconnect");
            _receivedCallback(bytes);
        }
    }
}