using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using Shared.Log;
using Shared.Tp.Util;

namespace Shared.Tp.Ext.Misc
{
    // Enough to keep 4 seconds in 60 fps sends.
    //  In case of long lag (>4 sec) rtt calculation will be wrong.
    //  However, we don't need more because history to restore the state cannot keep more than 1 second of states,
    //  and in the case rtt exceeds, we need to re-initialize the state.
    // 
    // Anyway (in the case of a requirement of 1000 fps sending),
    //  we can change this type either and/or its serialize/deserialize technique. 
    using TimeLinkLocalTicksIndex = Byte; 

    /// <summary>
    /// TODO: add api for client/server session to obtain "session time"
    /// TODO: make average and deviation RTT calculations, throw anything with 3-sigma rule
    /// TODO: make protocol more efficient using cyclic buffer to store local ticks
    ///     instead of sending and receiving it back with adjustment (send only index and adjustment)
    /// TODO: use SequenceReader or analog to read data
    /// TODO: efficient (lock-free) atomic change for _receivedRemote/_receivedLocal (fix rare wrong calculation on send)
    /// 
    /// </summary>
    public class TimeLink : ExtLink
    {
        // run-tick is current time measure //TODO: decide to make in ns instead of ms
        private const long TicksPerRt = TimeSpan.TicksPerMillisecond;
        private const long RtPerMs = 1;

        public class Api : ExtApi<TimeLink>
        {
            private readonly long _startTicks; //rt

            public Api(ITpApi innerApi) : base(innerApi)
            {
                _startTicks = DateTime.UtcNow.Ticks;
                Slog.Info($"start ticks: {_startTicks}");
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
                new(_startTicks) { Receiver = receiver };

            protected override TimeLink CreateServerLink(ITpLink innerLink) =>
                new(_startTicks) { InnerLink = innerLink };
        }

        private readonly long _startTicks;

        private TimeLinkLocalTicksIndex _receivedRemoteIdx;
        private long _receivedRemote; //rt

        private long _receivedLocal; //rt

        private int _rtt; //rt

        public TimeLink() { }
        private TimeLink(long startTicks) => _startTicks = startTicks;

        private long LocalRt // same as Api.LocalRt
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (DateTime.UtcNow.Ticks - _startTicks) / TicksPerRt;
        }

        public int LocalMs // same as Api.LocalMs
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

        private unsafe struct Details {
            private TimeLinkLocalTicksIndex _historyIndex;
            private fixed long _localTicksHistory[TimeLinkLocalTicksIndex.MaxValue + 1];

            public TimeLinkLocalTicksIndex AddLocalTicks(long localTicks)
            {
                var historyIndex = _historyIndex++;
                _localTicksHistory[historyIndex] = localTicks;
                return historyIndex;
            }

            public long GetLocalTicks(TimeLinkLocalTicksIndex historyIndex)
            {
                return _localTicksHistory[historyIndex];
            }
        }

        private Details _details;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void WriteTime(IBufferWriter<byte> writer)
        {
            var local = LocalRt;
            var localIdx = _details.AddLocalTicks(local);

            writer.Write(localIdx);
            writer.Write(local);

            //adjustment from previous reception, so the remote side can correctly calculate rtt
            var passedLocal = _receivedLocal != 0 ? local - _receivedLocal : 0;
            var receivedRemote = _receivedRemote;
            receivedRemote += passedLocal;
            writer.Write(_receivedRemoteIdx);
            writer.Write(receivedRemote);
            var receivedSendingDelta = (short)(local - _receivedLocal);
            writer.Write(receivedSendingDelta);

            //Slog.Info($"localIdx={localIdx:000} local={local} remoteAdjusted={receivedRemote} (passed={passedLocal})");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe ReadOnlySpan<byte> ReadTime(ReadOnlySpan<byte> span)
        {
            var local = LocalRt;

            const int length = 
                sizeof(TimeLinkLocalTicksIndex) + sizeof(long) +
                sizeof(TimeLinkLocalTicksIndex) + sizeof(long) +
                sizeof(short);
            var timeSpan = span[^length..];
            fixed (byte* ptrStart = timeSpan)
            {
                var ptr = ptrStart;
                _receivedRemoteIdx = Unsafe.ReadUnaligned<TimeLinkLocalTicksIndex>(ptr);
                ptr += sizeof(TimeLinkLocalTicksIndex);

                _receivedLocal = local;
                _receivedRemote = Unsafe.ReadUnaligned<long>(ptr);
                ptr += sizeof(long);

                var sentLocalIdx = Unsafe.ReadUnaligned<TimeLinkLocalTicksIndex>(ptr);
                ptr += sizeof(TimeLinkLocalTicksIndex);
                var sentLocal = _details.GetLocalTicks(sentLocalIdx);
                var rtt2 = (int)(local - sentLocal);

                var sentLocalAdjusted = Unsafe.ReadUnaligned<long>(ptr);
                ptr += sizeof(long);

                var receivedSendingDelta = Unsafe.ReadUnaligned<short>(ptr);
                //ptr += sizeof(short);
                rtt2 -= receivedSendingDelta;

                if (sentLocalAdjusted != 0)
                    _rtt = (int)(local - sentLocalAdjusted);

                Slog.Info($"remoteIdx={_receivedRemoteIdx:000} local={local} remote={_receivedRemote} sentLocalAdjusted={sentLocalAdjusted} rtt={_rtt} rtt2={rtt2}");
            }

            return span[..^length];
        }
    }
}