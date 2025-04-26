using System;
using System.Buffers;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared.Log;
using Shared.Tp.Util;
using UnityEngine;
using UnityEngine.Scripting;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Shared.Tp.Ext.Misc
{
    //TODO: create Mere link wrapper above Ext to simplify declaration of just send/receive even more
    //  possibly introduce separate LayerLink with custom processors list
    public class DumpLink : ExtLink
    {
        private readonly Api _api = null!;
        private static readonly UTF8Encoding _utf8Encoding = new(false, false);

        [Serializable]
        public class Options
        {
            [field: SerializeField] [RequiredMember]
            public bool LogEnabled { get; set; }
            
            [field: SerializeField] [RequiredMember]
            public int LogStartBytes  { get; set; } = 100;
            
            [field: SerializeField] [RequiredMember]
            public int LogEndBytes  { get; set; } = 20;
        }

        private readonly DumpStats _stats = new();
        public DumpStats Stats => _stats;

        public class Api : ExtApi<DumpLink>
        {
            private readonly ILogger _logger;
            internal ILogger Logger => _logger;

            private Options _options;
            internal Options Options => _options;

            public Api(
                ITpApi innerApi, 
                IOptionsMonitor<Options> options, 
                ILoggerFactory loggerFactory) 
                : base(innerApi)
            {
                _logger = loggerFactory.CreateLogger<DumpLink>();
                _options = options.CurrentValue;
                //TODO: dispose change tracking
                options.OnChange((o, _) => _options = o);
            }

            protected override DumpLink CreateClientLink(ITpReceiver receiver) 
                => new(this) { Receiver = receiver };
            protected override DumpLink CreateServerLink(ITpLink innerLink) 
                => new(this) { InnerLink = innerLink };
        }

        public DumpLink() { }
        private DumpLink(Api api) => _api = api;

        public override void Send<T>(TpWriteCb<T> writeCb, in T state)
        {
            base.Send(static (writer, s) =>
            {
                s.@this.Send(writer, s.writeCb, s.state);
            }, (@this: this, writeCb, state));
        }

        private void Send<T>(IBufferWriter<byte> writer, TpWriteCb<T> writeCb, in T state)
        {
            writeCb(writer, state);

            ReadOnlySpan<byte> span;
            switch (writer)
            {
                case ArrayBufferWriter<byte> arrayWriter:
                    span = arrayWriter.WrittenSpan;
                    break;
                case PooledBufferWriter pooledWriter:
                    span = pooledWriter.WrittenSpan;
                    break;
                default:
                    _api.Logger.Error($"unsupported buffer writer: {writer.GetType()}");
                    return;
            }

            _stats.Out.Add(span.Length);
            if (_api.Options.LogEnabled)
                Log(_api.Logger, _api.Options, span, "out", InnerLink.ToString());
        }

        public override void Received(ITpLink link, ReadOnlySpan<byte> span)
        {
            _stats.In.Add(span.Length);
            if (_api.Options.LogEnabled)
                Log(_api.Logger, _api.Options, span, "in", link.ToString());

            base.Received(link, span);
        }

        private static readonly char[] _midEllipsis = { ' ', '…', ' ' }; // '⋯' '…' Unicode Ellipsis
        private static void Log(ILogger logger, Options options, ReadOnlySpan<byte> span, string prefix, string member)
        {
            var startBytes = options.LogStartBytes;
            var endBytes = options.LogEndBytes;
            var maxBytes = startBytes + endBytes;
            Span<char> chars = stackalloc char[maxBytes + _midEllipsis.Length];
            int written;
            if (span.Length <= maxBytes)
            {
                written = Convert(span, chars);
            }
            else
            {
                written = Convert(span[..startBytes], chars);
                _midEllipsis.CopyTo(chars[written..]);
                written += _midEllipsis.Length;
                written += Convert(span[^endBytes..], chars[written..]);
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