using System;
using System.Diagnostics;
using System.Threading.Tasks;
using NUnit.Framework;
using Shared.Log;

namespace Shared.Tp.Tests
{
    public class TickerTests
    {
        [Test]
        public void Cycle_Diff()
        {
            unchecked
            {
                const ushort start = (ushort)-10;
                const ushort end = 10;
                const ushort diff = (ushort)(end - start);
                Assert.AreEqual(diff, 20);
            }
        }

        /// <summary>
        /// Just to diagnose accuracy (.net, editor, webgl)
        /// </summary>
        [Test]
        public async Task Delta_Impls()
        {
            var stampStart = Stopwatch.GetTimestamp();
            var ticksStart = DateTime.UtcNow.Ticks;

            await Task.Delay(500); // Thread.Sleep(500) doesn't work on WebGL

            var stampDelta = Stopwatch.GetTimestamp() - stampStart;
            // for (var i = 0; i < 100000; ++i) // additional small delta (just to make sure benchmarking works)
            //     stampDelta = Stopwatch.GetTimestamp() - stamp;
            var ticksDelta = DateTime.UtcNow.Ticks - ticksStart;

            var stampPerSeconds = Stopwatch.Frequency;
            var stampPerMs = stampPerSeconds / 1000;
            var stampMs = stampDelta / stampPerMs;
            Slog.Info($"stamp: start={stampStart} delta={stampDelta} freqMs={stampPerMs} ms={stampMs}");

            var ticksMs = ticksDelta / TimeSpan.TicksPerMillisecond;
            Slog.Info($"ticks: start={ticksStart} delta={ticksDelta} freqMs={TimeSpan.TicksPerMillisecond} ms={ticksMs}");

            Assert.That(stampMs, Is.InRange(490, 600));
            Assert.That(ticksMs, Is.InRange(490, 600));
        }
    }
}