using System.Threading;
using System.Threading.Tasks;

namespace Shared.Tp
{
    /// <summary>
    /// TODO: correctly handle exception on inner/outer calls
    /// </summary>
    public class ExtApi : ITpApi, ITpListener
    {
        private readonly ITpApi _innerApi;
        public ExtApi(ITpApi innerApi) => _innerApi = innerApi;

        async Task<ITpLink> ITpApi.Connect(ITpReceiver receiver, CancellationToken cancellationToken)
        {
            var clientLink = new Link { Receiver = receiver };
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
            var extLink = new Link { InnerLink = link };
            var receiver = _listener.Connected(extLink);
            if (receiver == null)
                return null;
            extLink.Receiver = receiver;
            return extLink;
        }

        private class Link: ITpLink, ITpReceiver
        {
            internal ITpLink InnerLink = null!;
            internal ITpReceiver Receiver = null!;

            public void Dispose() => InnerLink.Dispose();
            public string GetRemotePeerId() => "WRAP-" + InnerLink.GetRemotePeerId();
            public void Send(byte[] bytes) => InnerLink.Send(bytes);
            public void Received(ITpLink link, byte[]? bytes) => Receiver.Received(this, bytes);
        }
    }
}