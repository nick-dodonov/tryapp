using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Options;
using Shared.Log;
using Shared.Tp.Chrono;
using Shared.Tp.Util;
using Shared.Tp.Util.Stat;
using UnityEngine;
using UnityEngine.Scripting;

// ReSharper disable UseSymbolAlias

namespace Shared.Tp.Ext.Misc
{
    using LocalClock = DeltaClock<long, RawPeriod, RawClock>;
    using RemoteClock = DeltaClock<long, RawPeriod, RawClock>;

    // Enough to keep 4 seconds in 60 fps sends.
    //  In case of long lag (>4 sec) rtt calculation will be wrong.
    //  However, we don't need more because history to restore the state cannot keep more than 1 second of states,
    //  and in the case rtt exceeds, we need to re-initialize the state.
    // 
    // Anyway (in the case of a requirement of 1000 fps sending),
    //  we can change this type either and/or its serialize/deserialize technique. 
    using TimeTicksIndex = Byte; 

    /// <summary>
    /// TODO: use SequenceReader or analog to read data
    /// TODO: atomic for _receivedRemoteIdx/_receivedLocalRt (fix rare wrong adjustment calculation in send despite they are ok because of 3-sigma rule)
    /// 
    /// </summary>
    public class TimeLink : ExtLink
    {
        private static readonly Slog.Area _log = new();

        private readonly Api _api = null!;

        [Serializable]
        public class Options
        {
            [field: SerializeField] [RequiredMember]
            public bool LogWrite { get; set; }
            [field: SerializeField] [RequiredMember]
            public bool LogRead { get; set; }
            
            //TODO: move to client in case calculate not here
            [field: SerializeField] [RequiredMember]
            public bool LogHistory { get; set; }
            [field: SerializeField] [RequiredMember]
            public int HistoryOffsetMs;
        }

        public class Api : ExtApi<TimeLink>
        {
            private readonly LocalClock _localClock;

            private Options _options;
            internal Options Options => _options;

            public Api(
                ITpApi innerApi,
                IOptionsMonitor<Options> options) 
                : base(innerApi)
            {
                _options = options.CurrentValue;
                options.OnChange((o, _) => _options = o); //TODO: dispose change tracking

                _localClock = new(RawClock.Instance);
                Slog.Info($"start ticks: {_localClock.StartCount}");
            }

            public LocalClock LocalClock => _localClock;

            protected override TimeLink CreateClientLink(ITpReceiver receiver) =>
                new(this, _localClock) { Receiver = receiver };

            protected override TimeLink CreateServerLink(ITpLink innerLink) =>
                new(this, _localClock) { InnerLink = innerLink };
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

            public long GetLocalTicks(TimeTicksIndex historyIndex) => 
                _localTicksHistory[historyIndex];
        }

        private readonly LocalClock _localClock;
        private RemoteClock _remoteClock;

        private TimeTicksIndex _receivedRemoteIdx;
        private long _receivedLocalRt;

        private int _rttRt;
        private CycleSampleSet _rttRtSet;
        private CycleSampleSet _deltaRemoteRtSet;

        private Details _details;

        public TimeLink() { }
        private TimeLink(Api api, LocalClock localClock)
        {
            _api = api;
            _localClock = localClock;
            _remoteClock = new();
        }

        public LocalClock LocalClock => _localClock;
        public RemoteClock RemoteClock => _remoteClock;

        public int RttRt => _rttRt;
        public ref CycleSampleSet RttRtSet => ref _rttRtSet;

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
            var localRt = _localClock.RtCount();
            var localIdx = _details.AddLocalTicks(localRt);

            writer.Write(localIdx);
            writer.Write(localRt);

            writer.Write(_receivedRemoteIdx);

            // Adjustment from the previous reception allowing the remote side to correctly calculate RTT.
            // It's safe to occasionally send incorrect values
            //  (e.g., during lag or in debug, the delta can be much larger than ~1/min(receiveRate|sendRate) or even exceed ushort.MaxValue|~6.5sec),
            //  because the RTT sample set rejects such outliers using the 3-sigma rule.
            var receivedLocalRt = _receivedLocalRt;
            var receivedSentDeltaRt = receivedLocalRt > 0 // Skip only the initial incorrect value (to start filling the sample set with the correct ones)
                ? (ushort)(localRt - receivedLocalRt)
                : (ushort)0;
            writer.Write(receivedSentDeltaRt);

            if (_api.Options.LogWrite)
            {
                _log.Info( // ReSharper disable once ExplicitCallerInfoArgument
                    $"SL=({localIdx:000}){localRt,5} SdR=({_receivedRemoteIdx:000}){receivedSentDeltaRt,-4}",
                    $"W {localRt,5}"); //→
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe ReadOnlySpan<byte> ReadTime(ReadOnlySpan<byte> span)
        {
            var localRt = _localClock.RtCount();

            const int length = 
                sizeof(TimeTicksIndex) + sizeof(long) +
                sizeof(TimeTicksIndex) +
                sizeof(ushort);
            var timeSpan = span[^length..];
            fixed (byte* ptrStart = timeSpan)
            {
                var ptr = ptrStart;
                _receivedRemoteIdx = ReadUnaligned<TimeTicksIndex>(ref ptr);
                _receivedLocalRt = localRt;
                var receivedRemoteRt = ReadUnaligned<long>(ref ptr);

                var sentLocalIdx = ReadUnaligned<TimeTicksIndex>(ref ptr);
                var receivedSentDeltaRt = ReadUnaligned<ushort>(ref ptr);

                var sentLocalRt = _details.GetLocalTicks(sentLocalIdx);
                if (sentLocalRt > 0 && receivedSentDeltaRt > 0)
                {
                    _rttRt = (int)(localRt - sentLocalRt - receivedSentDeltaRt);
                    if (_rttRtSet.Check(_rttRt))
                        _rttRtSet.Add(_rttRt);
                }

                //TODO: correct remote offset with using smoothed value (and constraint it to never ever give ticks backward)
                var rttMean = _rttRtSet.MeanInt;
                var newRemoteRt = receivedRemoteRt + (rttMean >> 1);

                if (_api.Options.LogRead)
                {
                    var remoteRt = _remoteClock.Tick().RtTick().Count;
                    var deltaRemoteRt = newRemoteRt - remoteRt;

                    if (_remoteClock.StartCount != 0)
                        _deltaRemoteRtSet.Add((int)deltaRemoteRt);

                    _log.Info( // ReSharper disable once ExplicitCallerInfoArgument
                        $"RL=({sentLocalIdx:000}){sentLocalRt,5} RdR=({_receivedRemoteIdx:000}){receivedSentDeltaRt,-4} rtt={_rttRt,4}~{rttMean,-3}/{_rttRtSet.Count} RR={receivedRemoteRt,5} dR={deltaRemoteRt,4:+0;-0}~{_deltaRemoteRtSet.MeanInt:+0;-0}/{_deltaRemoteRtSet.Count}", 
                        $"R {localRt,5}"); //←
                }

                _remoteClock = new(RawClock.Instance, PeriodConverter<RtPeriod, RawPeriod>.Convert(newRemoteRt));
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