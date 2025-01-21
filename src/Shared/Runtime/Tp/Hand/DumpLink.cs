using System;
using System.Buffers;
using System.Text;
using Shared.Log;

namespace Shared.Tp.Hand
{
    public class DumpLink : ExtLink
    {
        private static readonly Slog.Area _log = new();
        private static readonly UTF8Encoding _utf8Encoding = new(false, false);

        public class Api : ExtApi<DumpLink>
        {
            public Api(ITpApi innerApi) : base(innerApi) { }
        }

        public override void Send<T>(TpWriteCb<T> writeCb, in T state)
        {
            base.Send(static (writer, s) =>
            {
                s.writeCb(writer, s.state);
                if (writer is ArrayBufferWriter<byte> arrayWriter)
                {
                    var writtenSpan = arrayWriter.WrittenSpan;

                    var maxBytesToConvert = Math.Min(writtenSpan.Length, 40);
                    Span<char> chars = stackalloc char[maxBytesToConvert];

                    var charsWritten = _utf8Encoding.GetChars(writtenSpan[..maxBytesToConvert], chars);
                    for (var i = 0; i < charsWritten; i++)
                    {
                        if (char.IsControl(chars[i]) || chars[i] == '\uFFFD') 
                            chars[i] = '\u00b7'; // “·” Unicode Replacement Character or control check
                    }

                    var ellipsis = maxBytesToConvert < writtenSpan.Length ? "...": null;
                    _log.Info($"out: [{writtenSpan.Length}] bytes: |{new string(chars[..charsWritten])}|{ellipsis}");
                }
                else
                {
                    _log.Error($"unsupported buffer writer: {writer.GetType()}");
                }
            }, (writeCb, state));
        }

        public override void Received(ITpLink link, ReadOnlySpan<byte> span)
        {
            base.Received(link, span);
        }
    }
}