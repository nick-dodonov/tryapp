using System;
using System.Buffers;
using System.Text;
using Microsoft.Extensions.Logging;
using Shared.Log;

namespace Shared.Tp.Hand
{
    public class DumpLink : ExtLink
    {
        private const int MaxLogBytes = 100;

        private readonly ILogger _logger = null!;
        private static readonly UTF8Encoding _utf8Encoding = new(false, false);

        public class Api : ExtApi<DumpLink>
        {
            private readonly ILogger _logger;
            public Api(ITpApi innerApi, ILogger logger) : base(innerApi) => _logger = logger;
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
                else
                    s._logger.Error($"unsupported buffer writer: {writer.GetType()}");
            }, (_logger, member: InnerLink.ToString(), writeCb, state));
        }

        public override void Received(ITpLink link, ReadOnlySpan<byte> span)
        {
            Log(_logger, span, "in", link.ToString());
            base.Received(link, span);
        }

        private static void Log(ILogger logger, ReadOnlySpan<byte> span, string prefix, string member)
        {
            var maxBytesToConvert = Math.Min(span.Length, MaxLogBytes);
            Span<char> chars = stackalloc char[maxBytesToConvert];

            var charsWritten = _utf8Encoding.GetChars(span[..maxBytesToConvert], chars);
            for (var i = 0; i < charsWritten; i++)
            {
                if (char.IsControl(chars[i]) || chars[i] == '\uFFFD')
                    chars[i] = '\u00b7'; // '·' Unicode Replacement Character or control check
            }

            var ellipsis = maxBytesToConvert < span.Length ? "…" : null; // '⋯' '…' Unicode Ellipsis
            var charsStr = new string(chars[..charsWritten]);
            logger.Info(
                $"{prefix}: [{span.Length}] bytes: {charsStr}{ellipsis}", //｟｠⦅⦆ «»
                member: member);
        }
    }
}