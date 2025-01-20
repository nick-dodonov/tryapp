using Shared.Log;

namespace Shared.Tp
{
    public class ExtLink: ITpLink, ITpReceiver
    {
        protected internal ITpLink InnerLink = null!;

        private ITpReceiver? _receiver;
        private PostponedBytes _receivePostponed; 
        protected internal ITpReceiver Receiver
        {
            set
            {
                _receiver = value;
                _receivePostponed.Feed(static (link, bytes) =>
                {
                    link._receiver!.Received(link, bytes);
                }, this);
            }
        }

        protected ExtLink() {} //empty constructor only for generic usage
        protected ExtLink(ITpLink innerLink) => InnerLink = innerLink;
        protected ExtLink(ITpReceiver receiver) => Receiver = receiver;

        public override string ToString() => $"{GetType().Name}(<{GetRemotePeerId()}>)"; //only for diagnostics

        public virtual void Dispose() => InnerLink.Dispose();
        public virtual string GetRemotePeerId() => InnerLink.GetRemotePeerId();
        public virtual void Send(byte[] bytes) => InnerLink.Send(bytes);
        public virtual void Received(ITpLink link, byte[]? bytes)
        {
            if (_receiver != null)
                _receiver.Received(this, bytes);
            else
            {
                Slog.Info($"{this}: no receiver: temping {bytes?.Length} bytes");
                _receivePostponed.Add(bytes);
            }
        }
    }
}