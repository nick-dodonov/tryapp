// ReSharper disable CheckNamespace

namespace System.Threading.Tasks
{
    /// <summary>
    /// Helper for Task.WaitAsync() helpers (introduced in net6 and not available in netstandard2.1)
    ///
    /// Adopted net9 methods as extensions methods for netstandard2.1 using TaskCompletionSource
    ///
    /// TODO: adopt for WebGL builds (ConfigureAwait isn't supported)
    /// TODO: possible speedup using custom GetAwaiter implementation
    /// TODO: add unit tests and comparison with core implementation behaviour
    /// </summary>
    public static class TaskExtensionsWaitAsync
    {
        private const uint UnsignedInfinite = unchecked((uint)-1);

        public static Task<TResult> WaitAsync<TResult>(this Task<TResult> task, CancellationToken cancellationToken)
            => WaitAsync(task, UnsignedInfinite, cancellationToken);
        public static Task<TResult> WaitAsync<TResult>(this Task<TResult> task, TimeSpan timeout)
            => WaitAsync(task, ValidateTimeout(timeout), CancellationToken.None);
        public static Task<TResult> WaitAsync<TResult>(this Task<TResult> task, TimeSpan timeout, CancellationToken cancellationToken)
            => WaitAsync(task, ValidateTimeout(timeout), cancellationToken);

        private static Task<TResult> WaitAsync<TResult>(Task<TResult> task, uint millisecondsTimeout,
            CancellationToken cancellationToken)
        {
            if (task.IsCompleted)
                return task;
            if (!cancellationToken.CanBeCanceled && millisecondsTimeout == UnsignedInfinite)
                return task;
            if (cancellationToken.IsCancellationRequested)
                return Task.FromCanceled<TResult>(cancellationToken);

            return millisecondsTimeout == 0
                ? Task.FromException<TResult>(new TimeoutException())
                : Core(task, millisecondsTimeout, cancellationToken);

            static async Task<TResult> Core(Task<TResult> task, uint millisecondsTimeout,
                CancellationToken cancellationToken)
            {
                var cts = CreateCancellationTokenSource(millisecondsTimeout, cancellationToken);
                var ct = cts.Token;
                using (cts)
                {
                    var cancellationTcs = new TaskCompletionSource<TResult>();
                    await using var _ = ct.Register(
                            () =>
                            {
                                IgnoreTaskException(task);
                                if (cancellationToken.IsCancellationRequested)
                                    cancellationTcs.TrySetCanceled(cancellationToken);
                                else
                                    cancellationTcs.TrySetException(new TimeoutException());
                            })
                        .ConfigureAwait(false);
                    var t = await Task
                        .WhenAny(task, cancellationTcs.Task)
                        .ConfigureAwait(false);
                    return t.GetAwaiter().GetResult();
                }
            }
        }

        public static Task WaitAsync(this Task task, CancellationToken cancellationToken)
            => WaitAsync(task, UnsignedInfinite, cancellationToken);
        public static Task WaitAsync(this Task task, TimeSpan timeout)
            => WaitAsync(task, ValidateTimeout(timeout), CancellationToken.None);
        public static Task WaitAsync(this Task task, TimeSpan timeout, CancellationToken cancellationToken)
            => WaitAsync(task, ValidateTimeout(timeout), cancellationToken);

        private static Task WaitAsync(Task task, uint millisecondsTimeout, CancellationToken cancellationToken)
        {
            if (task.IsCompleted)
                return task;
            if (!cancellationToken.CanBeCanceled && millisecondsTimeout == UnsignedInfinite)
                return task;
            if (cancellationToken.IsCancellationRequested)
                return Task.FromCanceled(cancellationToken);

            return millisecondsTimeout == 0
                ? Task.FromException(new TimeoutException())
                : Core(task, millisecondsTimeout, cancellationToken);

            static async Task Core(Task task, uint millisecondsTimeout, CancellationToken cancellationToken)
            {
                var cts = CreateCancellationTokenSource(millisecondsTimeout, cancellationToken);
                var ct = cts.Token;
                using (cts)
                {
                    var cancellationTcs = new TaskCompletionSource<object>();
                    await using var _ = ct.Register(
                            () =>
                            {
                                IgnoreTaskException(task);
                                if (cancellationToken.IsCancellationRequested)
                                    cancellationTcs.TrySetCanceled(cancellationToken);
                                else
                                    cancellationTcs.TrySetException(new TimeoutException());
                            })
                        .ConfigureAwait(false);
                    var t = await Task
                        .WhenAny(task, cancellationTcs.Task)
                        .ConfigureAwait(false);
                    t.GetAwaiter().GetResult();
                }
            }
        }

        private static uint ValidateTimeout(TimeSpan timeout)
        {
            var totalMilliseconds = (long)timeout.TotalMilliseconds;
            const uint maxSupportedTimeout = 0xfffffffe;
            if (totalMilliseconds is < -1 or > maxSupportedTimeout)
                throw new ArgumentOutOfRangeException(nameof(timeout));
            return (uint)totalMilliseconds;
        }

        private static CancellationTokenSource CreateCancellationTokenSource(uint millisecondsTimeout,
            CancellationToken cancellationToken)
        {
            if (!cancellationToken.CanBeCanceled) 
                return new((int)millisecondsTimeout);

            var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            if (millisecondsTimeout != UnsignedInfinite)
                cts.CancelAfter((int)millisecondsTimeout);
            return cts;
        }

        private static void IgnoreTaskException(Task task)
        {
            _ = task.ContinueWith(t => { _ = t.Exception; },
                TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously);
        }
    }
}