using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using Shared.Log;
using Shared.Tp.Util;

namespace Shared.Tp.Ext.Misc
{
    /// <summary>
    /// TODO: add api for client/server session to obtain "session time"
    /// TODO: make average and deviation RTT calculations, throw anything with 3-sigma rule
    /// TODO: make protocol more efficient using cyclic buffer to store local ticks
    ///     instead of sending and receiving it back with adjustment (send only index and adjustment)
    /// TODO: use SequenceReader or analog to read data
    /// TODO: efficient (lock-free) atomic change for _receivedRemote/_receivedLocal (fix rare wrong calculation on send)
    /// </summary>
    public class TimeLink : ExtLink
    {
        // run-tick is current time measure //TODO: decide to make in ns instead of ms
        private const long TicksPerRt = TimeSpan.TicksPerMillisecond;
        private const long RtPerMs = 1;

        public class Api : ExtApi<TimeLink>
        {
            private readonly ILogger _logger;
            private readonly long _startTicks; //rt

            public Api(ITpApi innerApi, ILoggerFactory loggerFactory) : base(innerApi)
            {
                _logger = loggerFactory.CreateLogger<TimeLink>();
                _startTicks = DateTime.UtcNow.Ticks;
                _logger.Info($"start ticks: {_startTicks}");
            }

            private long LocalRt
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => (DateTime.UtcNow.Ticks - _startTicks) / TicksPerRt;
            }

            public int LocalMs
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => (int)(LocalRt / RtPerMs);
            }

            protected override TimeLink CreateClientLink(ITpReceiver receiver) =>
                new(_startTicks, _logger) { Receiver = receiver };

            protected override TimeLink CreateServerLink(ITpLink innerLink) =>
                new(_startTicks, _logger) { InnerLink = innerLink };
        }

        private readonly ILogger _logger = null!;

        private readonly long _startTicks; //rt

        private long _receivedRemote; //rt
        private long _receivedLocal; //rt

        private int _rtt; //rt

        public TimeLink() { }

        private TimeLink(long startTicks, ILogger logger) =>
            (_startTicks, _logger) = (startTicks, logger);

        private long LocalRt
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (DateTime.UtcNow.Ticks - _startTicks) / TicksPerRt;
        }

        public int LocalMs
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (int)(LocalRt / RtPerMs);
        }

        public int RemoteMs
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (int)((LocalRt - _receivedLocal + _rtt / 2 + _receivedRemote) / RtPerMs);
        }

        public int RttMs
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (int)(_rtt / RtPerMs);
        }

        public override void Send<T>(TpWriteCb<T> writeCb, in T state)
        {
            base.Send(static (writer, s) =>
            {
                s.writeCb(writer, s.state);
                s.@this.WriteTime(writer);
            }, (@this: this, writeCb, state));
        }

        public override void Received(ITpLink link, ReadOnlySpan<byte> span)
        {
            span = ReadTime(span);
            base.Received(link, span);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void WriteTime(IBufferWriter<byte> writer)
        {
            var local = LocalRt;

            var passedLocal = _receivedLocal != 0 ? local - _receivedLocal : 0;
            writer.Write(local);

            //adjustment from previous receive, so remote side correctly calculates rtt
            var receivedRemote = _receivedRemote;
            receivedRemote += passedLocal;
            writer.Write(receivedRemote);

            //_logger.Info($"local={local} remoteAdjusted={receivedRemote} (passed={passedLocal})");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe ReadOnlySpan<byte> ReadTime(ReadOnlySpan<byte> span)
        {
            var local = LocalRt;

            const int length = sizeof(long) * 2;
            var timeSpan = span[^length..];
            fixed (byte* ptr = timeSpan)
            {
                _receivedLocal = local;
                _receivedRemote = Unsafe.ReadUnaligned<long>(ptr);
                var sentLocalAdjusted = Unsafe.ReadUnaligned<long>(ptr + sizeof(long));

                if (sentLocalAdjusted != 0)
                    _rtt = (int)(local - sentLocalAdjusted);

                //_logger.Info($"local={local} remote={_receivedRemote} sentLocalAdjusted={sentLocalAdjusted} (rtt={_rtt})");
            }

            return span[..^length];
        }
    }
}