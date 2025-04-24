using System;
using System.Threading;
using System.Threading.Tasks;
using Shared.Log;
using Shared.Tp;
using Shared.Web;

namespace Common.Logic.Shared
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
        private ITpLink _link;

        public ITpLink Link => _link;

        public StdCmdLink(ICmdReceiver<TReceive> receiver, ITpLink link)
        {
            _receiver = receiver;
            _link = link;
        }

        // client side
        public static async ValueTask<StdCmdLink<TSend, TReceive>> Connect(ICmdReceiver<TReceive> receiver, ITpApi api, CancellationToken cancellationToken)
        {
            var cmdLink = new StdCmdLink<TSend, TReceive>(receiver, null!);
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