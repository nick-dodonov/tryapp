using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Shared.Tp.Ext.Hand
{
    internal class HandSynState
    {
        private readonly HandshakeOptions _options;
        private readonly TaskCompletionSource<object?> _ackTcs = new();

        private int _attempts;
        private readonly long _startMs;

        public HandSynState(HandshakeOptions options)
        {
            _options = options;
            _startMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        public int Attempts => _attempts;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AckReceived()
        {
            _ackTcs.TrySetResult(null);
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
                var ackTask = _ackTcs.Task;

                // ack can already be received
                if (!ackTask.IsCompleted)
                    await ackTask.WaitAsync(TimeSpan.FromMilliseconds(attemptMs), cancellationToken);

                // ack received successfully - no need to resend
                return false;
            }
            catch (TimeoutException)
            {
                _attempts++;
            }

            if (attemptMs >= _options.SynRetryMs) 
                return true;

            throw new TimeoutException($"Handshake timeout: {_options.TimeoutMs}ms (syn attempts: {_attempts})");
        }
    }
}