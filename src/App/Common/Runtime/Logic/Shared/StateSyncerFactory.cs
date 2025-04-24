using System.Threading;
using System.Threading.Tasks;
using Shared.Tp;

namespace Common.Logic.Shared
{
    public static class StateSyncerFactory
    {
        public static StateSyncer<TLocal, TRemote> CreateConnected<TLocal, TRemote>(
            ISyncHandler<TLocal, TRemote> handler, 
            ITpLink link)
        {
            var syncer = new StateSyncer<TLocal, TRemote>(handler);
            syncer.SetCmdLink(new(syncer, link));
            return syncer;
        }

        public static async ValueTask<StateSyncer<TLocal, TRemote>> CreateAndConnect<TLocal, TRemote>(
            ISyncHandler<TLocal, TRemote> handler,
            ITpApi api, CancellationToken cancellationToken)
        {
            var syncer = new StateSyncer<TLocal, TRemote>(handler);
            syncer.SetCmdLink(await StdCmdLink<TLocal, TRemote>.Connect(syncer, api,cancellationToken));
            return syncer;
        }
    }
}