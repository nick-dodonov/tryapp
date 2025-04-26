using System;
using Shared.Log;
using Shared.Tp.Data;

namespace Shared.Tp.St.Cmd
{
    public interface ICmdSender<T>
    {
        void CmdSend(in T cmd);
    }

    public interface ICmdReceiver<T>
    {
        void CmdReceived(in T cmd);
        void CmdDisconnected();
    }

    public class CmdLink<TSend, TReceive> 
        : IDisposable
        , ICmdSender<TSend>
        , ITpReceiver
    {
        private readonly ICmdReceiver<TReceive> _receiver;
        private readonly IObjWriter<TSend> _writer;
        private readonly IObjReader<TReceive> _reader;
        private ITpLink _link = null!;

        public ITpLink Link => _link;

        internal CmdLink(ICmdReceiver<TReceive> receiver, IObjWriter<TSend> writer, IObjReader<TReceive> reader)
        {
            _receiver = receiver;
            _writer = writer;
            _reader = reader;
        }

        internal void SetLink(ITpLink link) 
            => _link = link;

        public void Dispose()
        {
            _link.Dispose();
        }

        public void CmdSend(in TSend cmd)
        {
            try
            {
                _link.Send(_writer.Serialize, in cmd);
            }
            catch (Exception e)
            {
                Slog.Error($"{e}");
            }
        }

        void ITpReceiver.Received(ITpLink link, ReadOnlySpan<byte> span)
        {
            try
            {
                var cmd = _reader.Deserialize(span);
                _receiver.CmdReceived(cmd);
            }
            catch (Exception e)
            {
                Slog.Error($"{e}");
            }
        }

        void ITpReceiver.Disconnected(ITpLink link)
        {
            try
            {
                _receiver.CmdDisconnected();
            }
            catch (Exception e)
            {
                Slog.Error($"{e}");
            }
        }
    }
}