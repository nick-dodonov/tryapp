using System;
using Shared.Log;
using Shared.Web;

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

    public class StdCmdLink<TSend, TReceive> 
        : IDisposable
        , ICmdSender<TSend>
        , ITpReceiver
    {
        private readonly ICmdReceiver<TReceive> _receiver;
        private ITpLink _link = null!;

        public ITpLink Link => _link;

        internal StdCmdLink(ICmdReceiver<TReceive> receiver) 
            => _receiver = receiver;
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
                _link.Send(WebSerializer.Default.Serialize, in cmd);
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
                var cmd = WebSerializer.Default.Deserialize<TReceive>(span);
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