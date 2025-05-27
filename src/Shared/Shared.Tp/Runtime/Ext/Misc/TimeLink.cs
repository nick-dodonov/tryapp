using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using Shared.Log;
using Shared.Tp.Util;
using Shared.Tp.Util.Stat;

// ReSharper disable UseSymbolAlias

namespace Shared.Tp.Ext.Misc
{
    // Enough to keep 4 seconds in 60 fps sends.
    //  In case of long lag (>4 sec) rtt calculation will be wrong.
    //  However, we don't need more because history to restore the state cannot keep more than 1 second of states,
    //  and in the case rtt exceeds, we need to re-initialize the state.
    // 
    // Anyway (in the case of a requirement of 1000 fps sending),
    //  we can change this type either and/or its serialize/deserialize technique. 
    using TimeTicksIndex = Byte; 

    /// <summary>
    /// TODO: add api for client/server session to obtain "session time"
    /// TODO: make average and deviation RTT calculations without exceptions by 3-sigma rule
    /// TODO: use SequenceReader or analog to read data
    /// TODO: efficient (lock-free) atomic change for _receivedRemote/_receivedLocal (fix rare wrong calculation on send)
    /// 
    /// </summary>
    public class TimeLink : ExtLink
    {
        public class Api : ExtApi<TimeLink>
        {
            private readonly Ticker _localTicker;

            public Api(ITpApi innerApi) : base(innerApi)
            {
                _localTicker = Ticker.StartNew();
                Slog.Info($"start ticks: {_localTicker.StartTicks}");
            }

            public Ticker LocalTicker => _localTicker;

            protected override TimeLink CreateClientLink(ITpReceiver receiver) =>
                new(_localTicker) { Receiver = receiver };

            protected override TimeLink CreateServerLink(ITpLink innerLink) =>
                new(_localTicker) { InnerLink = innerLink };
        }

        private unsafe struct Details {
            private TimeTicksIndex _historyIndex;
            private fixed long _localTicksHistory[TimeTicksIndex.MaxValue + 1];

            public TimeTicksIndex AddLocalTicks(long localTicks)
            {
                var historyIndex = _historyIndex++;
                _localTicksHistory[historyIndex] = localTicks;
                return historyIndex;
            }

            public long GetLocalTicks(TimeTicksIndex historyIndex)
            {
                return _localTicksHistory[historyIndex];
            }
        }

        private readonly Ticker _localTicker;

        private TimeTicksIndex _receivedRemoteIdx;

        private long _receivedRemoteRt;
        private long _receivedLocalRt;

        private int _rttRt;
        private CycleSampleSet _rttRtSet;

        private Details _details;
        
        public TimeLink() { }
        private TimeLink(Ticker localTicker) => _localTicker = localTicker;

        public Ticker LocalTicker => _localTicker;

        public int RemoteMs
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (int)((_localTicker.Rt - _receivedLocalRt + _rttRt / 2 + _receivedRemoteRt) / Ticker.RtPerMs);
        }

        public int RttRt
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _rttRt;
        }

        public float RttRtMean
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _rttRtSet.Mean;
        }

        public float RttRtStdDev
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _rttRtSet.StdDeviation;
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
            var localRt = _localTicker.Rt;
            var localIdx = _details.AddLocalTicks(localRt);

            writer.Write(localIdx);
            writer.Write(localRt);

            writer.Write(_receivedRemoteIdx);

            //adjustment from previous reception, so the remote side can correctly calculate rtt
            var receivedLocalRt = _receivedLocalRt;
            var receivedSentDeltaRt = receivedLocalRt > 0 ? (short)(localRt - receivedLocalRt): (short)0;
            writer.Write(receivedSentDeltaRt);

            //Slog.Info($"localIdx={localIdx:000} local={local} remoteAdjusted={receivedRemote} (passed={passedLocal})");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe ReadOnlySpan<byte> ReadTime(ReadOnlySpan<byte> span)
        {
            var localRt = _localTicker.Rt;

            const int length = 
                sizeof(TimeTicksIndex) + sizeof(long) +
                sizeof(TimeTicksIndex) +
                sizeof(short);
            var timeSpan = span[^length..];
            fixed (byte* ptrStart = timeSpan)
            {
                var ptr = ptrStart;
                _receivedRemoteIdx = ReadUnaligned<TimeTicksIndex>(ref ptr);
                _receivedRemoteRt = ReadUnaligned<long>(ref ptr);
                _receivedLocalRt = localRt;

                var sentLocalIdx = ReadUnaligned<TimeTicksIndex>(ref ptr);

                var receivedSentDeltaRt = ReadUnaligned<short>(ref ptr);

                var sentLocalRt = _details.GetLocalTicks(sentLocalIdx);
                if (sentLocalRt > 0 && receivedSentDeltaRt > 0)
                {
                    _rttRt = (int)(localRt - sentLocalRt - receivedSentDeltaRt);
                    _rttRtSet.Add(_rttRt);
                }

                //Slog.Info($"remoteIdx={_receivedRemoteIdx:000} local={local} remote={_receivedRemoteRt} rtt={_rttRt}");
            }

            return span[..^length];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe T ReadUnaligned<T>(ref byte* ptr) where T : unmanaged
        {
            var result = Unsafe.ReadUnaligned<T>(ptr);
            ptr += sizeof(T);
            return result;
        }
    }
}