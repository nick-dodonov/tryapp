using System;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using Shared.Log;
using Shared.Tp.Util;

namespace Shared.Tp.Ext.Misc
{
    /// <summary>
    /// TODO: use SequenceReader or analog to read data 
    /// </summary>
    public class TimeLink : ExtLink
    {
        public class Api : ExtApi<TimeLink>
        {
            private const long TicksPerNs = TimeSpan.TicksPerMillisecond;

            private readonly ILogger _logger;
            private readonly long _startNs;

            public Api(ITpApi innerApi, ILoggerFactory loggerFactory) : base(innerApi)
            {
                _logger = loggerFactory.CreateLogger<TimeLink>();
                _startNs = DateTime.UtcNow.Ticks / TicksPerNs;
                _logger.Info($"start ns: {_startNs}");
            }

            public long LocalNs => DateTime.UtcNow.Ticks / TicksPerNs - _startNs;

            protected override TimeLink CreateClientLink(ITpReceiver receiver) => 
                new(this, _logger) { Receiver = receiver };
            protected override TimeLink CreateServerLink(ITpLink innerLink) => 
                new(this, _logger) { InnerLink = innerLink };
        }

        private readonly ILogger _logger = null!;
        private readonly Api _api = null!;

        public TimeLink() { }
        private TimeLink(Api api, ILogger logger) => (_api, _logger) = (api, logger);

        public override void Send<T>(TpWriteCb<T> writeCb, in T state)
        {
            base.Send(static (writer, s) =>
            {
                s.writeCb(writer, s.state);

                var localNs = s._api.LocalNs;
                s._logger.Info($"local ns: {localNs}");
                writer.Write(localNs);
            }, (_logger, _api, writeCb, state));
        }

        public override unsafe void Received(ITpLink link, ReadOnlySpan<byte> span)
        {
            var timeSpan = span[^sizeof(long)..];
            fixed (byte* ptr = timeSpan)
            {
                var remoteNs = Unsafe.ReadUnaligned<long>(ptr);
                _logger.Info($"remote ns: {remoteNs}");
            }

            span = span[..^sizeof(long)];
            base.Received(link, span);
        }
    }
}