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
        private ITpListener? _listener;

        protected ExtApi(ITpApi innerApi) => _innerApi = innerApi;

        protected virtual TLink CreateClientLink(ITpReceiver receiver) => new() { Receiver = receiver };
        public virtual async ValueTask<ITpLink> Connect(ITpReceiver receiver, CancellationToken cancellationToken)
        {
            var clientLink = CreateClientLink(receiver);
            clientLink.InnerLink = await _innerApi.Connect(clientLink, cancellationToken);
            return clientLink;
        }

        public virtual void Listen(ITpListener listener)
        {
            _listener = listener;
            _innerApi.Listen(this);
        }

        protected virtual TLink CreateServerLink(ITpLink innerLink) => new() { InnerLink = innerLink };
        public virtual ITpReceiver? Connected(ITpLink link)
        {
            if (_listener == null)
                return null;
            var extLink = CreateServerLink(link);
            var receiver = _listener.Connected(extLink);
            if (receiver == null)
                return null;
            extLink.Receiver = receiver;
            return extLink;
        }
    }

    public class ExtLink: ITpLink, ITpReceiver
    {
        protected internal ITpLink InnerLink = null!;
        protected internal ITpReceiver Receiver = null!;

        protected ExtLink() {} //empty constructor only for generic usage
        protected ExtLink(ITpLink innerLink) => InnerLink = innerLink;
        protected ExtLink(ITpReceiver receiver) => Receiver = receiver;

        public override string ToString() => $"{GetType().Name}({GetRemotePeerId()})"; //only for diagnostics

        public virtual void Dispose() => InnerLink.Dispose();
        public virtual string GetRemotePeerId() => InnerLink.GetRemotePeerId();
        public virtual void Send(byte[] bytes) => InnerLink.Send(bytes);
        public virtual void Received(ITpLink link, byte[]? bytes) => Receiver.Received(this, bytes);
    }
}