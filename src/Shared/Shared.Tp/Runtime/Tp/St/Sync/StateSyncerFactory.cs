using System.Threading;
using System.Threading.Tasks;
using Shared.Tp.St.Cmd;

namespace Shared.Tp.St.Sync
{
    public static class StateSyncerFactory
    {
        public static StateSyncer<TLocal, TRemote> CreateConnected<TLocal, TRemote>(
            ISyncHandler<TLocal, TRemote> handler, 
            ITpLink link)
        {
            var syncer = new StateSyncer<TLocal, TRemote>(handler);
            var cmdLink = CmdLinkFactory<TLocal, TRemote>.CreateConnected(syncer, link);
            syncer.SetCmdLink(cmdLink);
            return syncer;
        }

        public static async ValueTask<StateSyncer<TLocal, TRemote>> CreateAndConnect<TLocal, TRemote>(
            ISyncHandler<TLocal, TRemote> handler,
            ITpApi api, CancellationToken cancellationToken)
        {
            var syncer = new StateSyncer<TLocal, TRemote>(handler);
            var cmdLink = await CmdLinkFactory<TLocal, TRemote>.CreateAndConnect(syncer, api,cancellationToken);
            syncer.SetCmdLink(cmdLink);
            return syncer;
        }
    }
}