using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Diagnostics.Debug;
using Shared.Log;
using UnityEngine.Scripting;

// ReSharper disable AsyncVoidMethod
// ReSharper disable MethodSupportsCancellation

namespace Diagnostics
{
    public static class TryAsync
    {
        private static readonly Slog.Area _log = new();

        private static Timer _timer;
        [Preserve, DebugAction]
        public static void TryTimer()
        {
            var logScope = new LogTimeScope("cts-ms");
            _timer = new(TryTimerCallback, logScope, 1000, -1);
        }

        private static void TryTimerCallback(object state)
        {
            var logScope = (LogTimeScope)state;
            logScope.Dispose();
            _timer.Dispose();
            _timer = null;
        }

        [Preserve, DebugAction]
        public static async void CtsMs()
        {
            using var _ = new LogTimeScope("cts-ms");
            using var cts = new CancellationTokenSource(1000);
            await Task.Delay(5000, cts.Token);
        }

        [Preserve, DebugAction]
        public static async void CtsLinked()
        {
            using var _ = new LogTimeScope("cts-linked");
            using var cts = new CancellationTokenSource(1000);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cts.Token);
            await Task.Delay(5000, linkedCts.Token);
        }

        [Preserve, DebugAction]
        public static async void WaitAsyncMs()
        {
            using var _ = new LogTimeScope("wait-async-ms");
            await Task.Delay(5000).WaitAsync(TimeSpan.FromMilliseconds(1000));
        }

        [Preserve, DebugAction]
        public static async void WaitAsyncCt()
        {
            using var _ = new LogTimeScope("wait-async-ms");
            using var cts = new CancellationTokenSource(1000);
            await Task.Delay(5000).WaitAsync(cts.Token);
        }

        [Preserve, DebugAction]
        public static async void WaitAsyncBothMs()
        {
            using var _ = new LogTimeScope("wait-async-both-ms");
            using var cts = new CancellationTokenSource(3000);
            await Task.Delay(5000).WaitAsync(TimeSpan.FromMilliseconds(1000), cts.Token);
        }

        [Preserve, DebugAction]
        public static async void WaitAsyncBothCt()
        {
            using var _ = new LogTimeScope("wait-async-both-ct");
            using var cts = new CancellationTokenSource(1000);
            await Task.Delay(5000).WaitAsync(TimeSpan.FromMilliseconds(3000), cts.Token);
        }

        private class LogTimeScope : IDisposable
        {
            private readonly string _context;
            private readonly string _member;
            private readonly long _start;

            public LogTimeScope(string context, [CallerMemberName] string member = "")
            {
                _context = context;
                _member = member;
                _start = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            }

            public void Dispose()
            {
                var elapsedMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - _start;
                _log.Info($"{_context}: elapsedMs: {elapsedMs}ms", member: _member);
            }
        }
    }
}