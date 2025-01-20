using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Shared.Log;

namespace Common.Logic
{
    internal struct PeerSynState
    {
        private HandshakeOptions _options;
        private TaskCompletionSource<object?>? _ackTcs; //null means ack already received

        private int _attempts;
        private long _startMs;

        public void Reset(HandshakeOptions options)
        {
            _options = options;

            _ackTcs?.TrySetCanceled();
            _ackTcs = new();

            _attempts = 0;
            _startMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AckReceived()
        {
            if (_ackTcs == null)
                return;

            Slog.Info("ack received");
            _ackTcs.TrySetResult(null);
            _ackTcs = null;
        }

        public async Task<bool> AwaitResend(CancellationToken cancellationToken)
        {
            var elapsedMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - _startMs;
            var remainingMs = _options.TimeoutMs - elapsedMs;
            long attemptMs = _options.SynRetryMs;
            if (attemptMs > remainingMs)
                attemptMs = remainingMs;
            try
            {
                var ackTask = _ackTcs?.Task;
                if (ackTask is { IsCompleted: false }) // ack can already be received
                    await ackTask.WaitAsync(TimeSpan.FromMilliseconds(attemptMs), cancellationToken);
                return false; // ack received successfully - no need to resend
            }
            catch (TimeoutException)
            {
                _attempts++;
            }

            if (attemptMs >= _options.SynRetryMs) 
                return true;

            var message = $"Handshake timeout: {_options.TimeoutMs}ms (attempts: {_attempts})";
            Slog.Warn(message);
            throw new TimeoutException(message);
        }
    }
}