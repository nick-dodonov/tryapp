using System;
using System.Threading;
using System.Threading.Tasks;
using Shared.Log;
using Shared.Tp;
using Shared.Web;

namespace Common.Logic
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
        private ITpLink _link;
        private readonly ICmdReceiver<TReceive> _receiver;

        public ITpLink Link => _link;

        public StdCmdLink(ITpLink link, ICmdReceiver<TReceive> receiver)
        {
            _link = link;
            _receiver = receiver;
        }

        // client side
        public static async ValueTask<StdCmdLink<TSend, TReceive>> Connect(ITpApi api, ICmdReceiver<TReceive> receiver, CancellationToken cancellationToken)
        {
            var cmdLink = new StdCmdLink<TSend, TReceive>(null!, receiver);
            cmdLink._link = await api.Connect(cmdLink, cancellationToken);
            return cmdLink;
        }

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