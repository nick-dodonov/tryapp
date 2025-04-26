using System.Threading;
using System.Threading.Tasks;
using Shared.Tp.Data;

namespace Shared.Tp.St.Cmd
{
    public class CmdLinkFactory<TSend, TReceive>
    {
        private readonly IObjWriter<TSend> _writer;
        private readonly IObjReader<TReceive> _reader;

        public CmdLinkFactory(IObjWriter<TSend> writer, IObjReader<TReceive> reader)
        {
            _writer = writer;
            _reader = reader;
        }

        public CmdLink<TSend, TReceive> CreateConnected(ICmdReceiver<TReceive> receiver, ITpLink link)
        {
            var cmdLink = new CmdLink<TSend, TReceive>(receiver, _writer, _reader);
            cmdLink.SetLink(link);
            return cmdLink;
        }

        public async ValueTask<CmdLink<TSend, TReceive>> CreateAndConnect(ICmdReceiver<TReceive> receiver, ITpApi api, CancellationToken cancellationToken)
        {
            var cmdLink = new CmdLink<TSend, TReceive>(receiver, _writer, _reader);
            var link = await api.Connect(cmdLink, cancellationToken);
            cmdLink.SetLink(link);
            return cmdLink;
        }
    }
}