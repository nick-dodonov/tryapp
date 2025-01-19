using System.Threading;
using System.Threading.Tasks;

namespace Shared.Tp
{
    /// <summary>
    /// TODO: correctly handle exception on inner/outer calls
    /// </summary>
    public class ExtApi<TLink> : ITpApi, ITpListener where TLink: ExtLink, new()
    {
        private readonly ITpApi _innerApi;
        public ExtApi(ITpApi innerApi) => _innerApi = innerApi;

        async Task<ITpLink> ITpApi.Connect(ITpReceiver receiver, CancellationToken cancellationToken)
        {
            var clientLink = new TLink { Receiver = receiver };
            clientLink.InnerLink = await _innerApi.Connect(clientLink, cancellationToken);
            return clientLink;
        }

        private ITpListener? _listener;

        void ITpApi.Listen(ITpListener listener)
        {
            _listener = listener;
            _innerApi.Listen(this);
        }

        ITpReceiver? ITpListener.Connected(ITpLink link)
        {
            if (_listener == null)
                return null;
            var extLink = new TLink { InnerLink = link };
            var receiver = _listener.Connected(extLink);
            if (receiver == null)
                return null;
            extLink.Receiver = receiver;
            return extLink;
        }
    }
    
    public class ExtLink: ITpLink, ITpReceiver
    {
        internal ITpLink InnerLink = null!;
        internal ITpReceiver Receiver = null!;

        public virtual void Dispose() => InnerLink.Dispose();
        public virtual string GetRemotePeerId() => InnerLink.GetRemotePeerId();
        public virtual void Send(byte[] bytes) => InnerLink.Send(bytes);
        public virtual void Received(ITpLink link, byte[]? bytes) => Receiver.Received(this, bytes);
    }
}