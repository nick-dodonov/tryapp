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
        protected readonly Slog.Area Log;

        private readonly IRtcService _service;
        private readonly ITpReceiver _receiver;

        private int _linkId = -1; // -1 until offer is not obtained
        private string? _linkToken; // null until offer is not obtained

        protected int LinkId => _linkId;

        public abstract void Dispose();
        public abstract string GetRemotePeerId();

        //public abstract void Send(ReadOnlySpan<byte> span);
        public abstract void Send<T>(TpWriteCb<T> writeCb, in T state);

        protected BaseRtcLink(IRtcService service, ITpReceiver receiver)
        {
            Log = new(GetType().Name);

            _service = service;
            _receiver = receiver;
        }

        protected async Task<RtcOffer> ObtainOffer(CancellationToken cancellationToken)
        {
            Log.Info("request");
            var offer = await _service.GetOffer(cancellationToken);
            Log.Info($"result: {offer}");

            _linkId = offer.LinkId;
            Log.AddCategorySuffix($" <{_linkId}>");
            _linkToken = offer.LinkToken;

            return offer;
        }

        protected async Task<RtcIcInit[]> ReportAnswer(RtcSdpInit answer, CancellationToken cancellationToken)
        {
            Log.Info($"request: {answer}");
            var candidates = await _service.SetAnswer(_linkToken!, answer, cancellationToken);
            Log.Info($"result: [{candidates.Length}] candidates:\n{string.Join('\n', candidates)}");
            return candidates;
        }

        protected async Task ReportIceCandidates(RtcIcInit[] candidates, CancellationToken cancellationToken)
        {
            Log.Info($"request: [{candidates.Length}] candidates:\n{string.Join('\n', candidates)}");
            await _service.AddIceCandidates(_linkToken!, candidates, cancellationToken);
            Log.Info("complete");
        }

        protected void CallReceived(byte[]? bytes)
        {
            if (bytes == null)
                Log.Info("disconnected");

            if (bytes != null)
                _receiver.Received(this, bytes);
            else
                _receiver.Disconnected(this);
            // if (_receiver == null)
            //     Log.Error("receiver is not set: TODO: postpone");
        }
    }
}