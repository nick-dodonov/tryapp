using System;
using Microsoft.Extensions.Logging;
using Shared.Log;

namespace Shared.Tp.Ext.Misc
{
    //TODO: create Mere link wrapper above Ext to simplify declaration of just send/receive even more
    //  possibly introduce separate LayerLink with custom processors list
    public class TimeLink : ExtLink
    {
        private readonly ILogger _logger = null!;

        public class Api : ExtApi<TimeLink>
        {
            private readonly ILogger _logger;

            public Api(ITpApi innerApi, ILoggerFactory loggerFactory) : base(innerApi) =>
                _logger = loggerFactory.CreateLogger<TimeLink>();

            protected override TimeLink CreateClientLink(ITpReceiver receiver) => new(_logger) { Receiver = receiver };
            protected override TimeLink CreateServerLink(ITpLink innerLink) => new(_logger) { InnerLink = innerLink };
        }

        public TimeLink() { }
        private TimeLink(ILogger logger) => _logger = logger;

        public override void Send<T>(TpWriteCb<T> writeCb, in T state)
        {
            base.Send(static (writer, s) =>
            {
                s.writeCb(writer, s.state);
                s._logger.Error("SSSSSSSSSSSS");
            }, (_logger, member: InnerLink.ToString(), writeCb, state));
        }

        public override void Received(ITpLink link, ReadOnlySpan<byte> span)
        {
            _logger.Info("RRRRRRRRRRR");
            base.Received(link, span);
        }
    }
}