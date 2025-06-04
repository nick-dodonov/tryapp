using System;
using System.Diagnostics;
using System.Threading.Tasks;
using NUnit.Framework;
using Shared.Log;
using Shared.Tp.Ext.Misc;

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

        [Test]
        public void Cycle_Impl()
        {
            const byte maxCycleTest = 0xF;
            var values = new[] { 0, 0, 10, 20, 30, 40, 0, 0, 14, 15 };
            byte prevCycle = 0;
            foreach (var value in values)
            {
                var cycle = (byte)(value % maxCycleTest + 1);
                var diff = (byte)((cycle - prevCycle + maxCycleTest) % maxCycleTest);
                Slog.Info($"{value} -> {cycle} {(prevCycle != 0 ? $"diff={diff}": null)}");
                prevCycle = cycle;
            }
        }

        [Test]
        public void Point_Conversions()
        {
            //const int expectedRt = 100_000;
            var ticks = 
                //Ticker.TicksPerRt * expectedRt +
                Ticker.TicksPerRt * Ticker.MaxCycledRt +
                Ticker.TicksPerRt * (Ticker.MaxCycledRt + 1) * 17 + 
                Ticker.TicksPerRt / 3;
            Slog.Info($"ticks={ticks} TicksPerRt={Ticker.TicksPerRt}");

            var tp = new TickPoint(ticks);

            Slog.Info($"Rt={tp.Rt}");
            Slog.Info($"CycledRt={tp.CycledRt}");
            Slog.Info($"RtFraction={tp.RtFraction}");
            
            Slog.Info($"Seconds={tp.Seconds}");
            Slog.Info($"CycledSeconds={tp.CycledSeconds}");
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