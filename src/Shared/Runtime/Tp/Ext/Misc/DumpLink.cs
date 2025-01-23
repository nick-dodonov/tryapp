using System;
using System.Buffers;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared.Log;
using Shared.Tp.Util;

namespace Shared.Tp.Ext.Misc
{
    //TODO: create Mere link wrapper above Ext to simplify declaration of just send/receive even more
    //  possibly introduce separate LayerLink with custom processors list
    public class DumpLink : ExtLink
    {
        private const int StartBytes = 100;
        private const int EndBytes = 20;

        private readonly ILogger _logger = null!;
        private static readonly UTF8Encoding _utf8Encoding = new(false, false);

        public class Options
        {
            public bool Enabled { get; set; }
        }

        public class Api : ExtApi<DumpLink>
        {
            private readonly ILogger _logger;
            private readonly IOptionsMonitor<Options> _optionsMonitor;

            public Api(ITpApi innerApi, IOptionsMonitor<Options> optionsMonitor, ILoggerFactory loggerFactory) 
                : base(innerApi)
            {
                _logger = loggerFactory.CreateLogger<DumpLink>();
                _optionsMonitor = optionsMonitor;
                _logger.Info($"options.Enabled: {_optionsMonitor.CurrentValue.Enabled}");
            }

            protected override DumpLink CreateClientLink(ITpReceiver receiver) => new(_logger) { Receiver = receiver };
            protected override DumpLink CreateServerLink(ITpLink innerLink) => new(_logger) { InnerLink = innerLink };
        }

        public DumpLink() { }
        private DumpLink(ILogger logger) => _logger = logger;

        public override void Send<T>(TpWriteCb<T> writeCb, in T state)
        {
            base.Send(static (writer, s) =>
            {
                s.writeCb(writer, s.state);
                if (writer is ArrayBufferWriter<byte> arrayWriter)
                    Log(s._logger, arrayWriter.WrittenSpan, "out", s.member);
                else if (writer is PooledBufferWriter pooledWriter)
                    Log(s._logger, pooledWriter.WrittenSpan, "out", s.member);
                else
                    s._logger.Error($"unsupported buffer writer: {writer.GetType()}");
            }, (_logger, member: InnerLink.ToString(), writeCb, state));
        }

        public override void Received(ITpLink link, ReadOnlySpan<byte> span)
        {
            Log(_logger, span, "in", link.ToString());
            base.Received(link, span);
        }

        private static readonly char[] _midEllipsis = { ' ', '…', ' ' }; // '⋯' '…' Unicode Ellipsis
        private static void Log(ILogger logger, ReadOnlySpan<byte> span, string prefix, string member)
        {
            const int maxBytes = StartBytes + EndBytes;
            Span<char> chars = stackalloc char[maxBytes + _midEllipsis.Length];
            int written;
            if (span.Length <= maxBytes)
            {
                written = Convert(span, chars);
            }
            else
            {
                written = Convert(span[..StartBytes], chars);
                _midEllipsis.CopyTo(chars[written..]);
                written += _midEllipsis.Length;
                written += Convert(span[^EndBytes..], chars[written..]);
            }

            var charsStr = new string(chars[..written]);
            logger.Info($"{prefix}: [{span.Length}] bytes: {charsStr}", member: member); //｟｠⦅⦆ «»
            return;

            static int Convert(ReadOnlySpan<byte> span, Span<char> chars)
            {
                var charsWritten = _utf8Encoding.GetChars(span, chars);
                for (var i = 0; i < charsWritten; i++)
                {
                    if (char.IsControl(chars[i]) || chars[i] == '\uFFFD')
                        chars[i] = '\u00b7'; // '·' Unicode Replacement Character or control check
                }

                return charsWritten;
            }
        }
    }
}