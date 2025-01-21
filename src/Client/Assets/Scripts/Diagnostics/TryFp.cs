using System;
using System.Buffers;
using Shared.Log;

namespace Diagnostics
{
    public unsafe class TryFp
    {
        ///////////////////////////////////////////////////////////////////////////////////////
        /// old way via callback or function pointer (doesn't support spans)
        private delegate void WriteCb<T>(IBufferWriter<byte> writer, in T value);
        private static void WriteByCb<T>(T value, WriteCb<T> callback)
        {
            Slog.Info(".");
            var writer = new ArrayBufferWriter<byte>();
            callback(writer, value);
        }
        private static void WriteByFpCb<T>(T value, delegate*<IBufferWriter<byte>, T, void> callback)
        {
            Slog.Info(".");
            var writer = new ArrayBufferWriter<byte>();
            callback(writer, value);
        }
        private static void Write(IBufferWriter<byte> writer, ReadOnlyMemory<byte> value) => Slog.Info(".");
        private static void Foo()
        {
            Slog.Info(".");
            var bytes = new byte[10];
            var obj = new ReadOnlyMemory<byte>(bytes);
            WriteByFpCb(obj, &Write);
        }
        
        ///////////////////////////////////////////////////////////////////////////////////////
        /// try new way via using (support spans but complicated extensions)
        public readonly ref struct Sender
        {
            private readonly ILink _link;
            public readonly IBufferWriter<byte> Writer;

            private readonly CompleteCb _completeCb;

            public delegate void CompleteCb(in Sender sender);
            public Sender(ILink link, IBufferWriter<byte> writer, CompleteCb completeCb)
            {
                _link = link;
                Writer = writer;
                _completeCb = completeCb;
            }

            public void Dispose()
            {
                _completeCb(this);
            }
        }

        public interface ILink
        {
            public Sender Send();
        }

        private class Link : ILink
        {
            Sender ILink.Send()
            {
                var writer = new ArrayBufferWriter<byte>();
                return new(this, writer, SendComplete);
            }

            private static void SendComplete(in Sender sender)
            {
                Slog.Info(".");
            }
        }

        private class WrapLink : ILink
        {
            private readonly ILink _innerLink;
            public WrapLink(ILink innerLink) => _innerLink = innerLink;

            public Sender Send()
            {
                Slog.Info("WrapLink");
                using var innerSender = _innerLink.Send();
                innerSender.Writer.Write(new byte[10]); //prefix
                return new(this, innerSender.Writer, SendComplete);
            }
            
            private static void SendComplete(in Sender sender)
            {
                Slog.Info(".");
            }
        }

        private static void TrySend()
        {
            ILink link = new WrapLink(new Link());
            using var sender = link.Send();
            sender.Writer.Advance(17);
        }
    }
}