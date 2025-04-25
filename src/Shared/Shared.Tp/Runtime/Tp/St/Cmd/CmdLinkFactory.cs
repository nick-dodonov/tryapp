using System.Threading;
using System.Threading.Tasks;

namespace Shared.Tp.St.Cmd
{
    public static class CmdLinkFactory<TSend, TReceive>
    {
        public static StdCmdLink<TSend, TReceive> CreateConnected(ICmdReceiver<TReceive> receiver, ITpLink link)
        {
            var cmdLink = new StdCmdLink<TSend, TReceive>(receiver);
            cmdLink.SetLink(link);
            return cmdLink;
        }

        public static async ValueTask<StdCmdLink<TSend, TReceive>> CreateAndConnect(ICmdReceiver<TReceive> receiver, ITpApi api, CancellationToken cancellationToken)
        {
            var cmdLink = new StdCmdLink<TSend, TReceive>(receiver);
            var link = await api.Connect(cmdLink, cancellationToken);
            cmdLink.SetLink(link);
            return cmdLink;
        }
    }
}