using System;
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
                    if (bytes != null)
                        link._receiver!.Received(link, bytes);
                    else
                        link._receiver!.Disconnected(link);
                }, this);
            }
        }

        protected ExtLink() {} //empty constructor only for generic usage
        protected ExtLink(ITpLink innerLink) => InnerLink = innerLink;
        protected ExtLink(ITpReceiver receiver) => Receiver = receiver;

        public override string ToString() => $"{GetType().Name}(<{GetRemotePeerId()}>)"; //only for diagnostics

        protected virtual void Close(string reason) => InnerLink.Dispose();
        public virtual void Dispose() => Close("disposing");

        public virtual string GetRemotePeerId() => InnerLink.GetRemotePeerId();
        public virtual void Send(byte[] bytes) => InnerLink.Send(bytes);
        public virtual void Received(ITpLink link, ReadOnlySpan<byte> span)
        {
            if (_receiver != null)
                _receiver.Received(this, span);
            else
            {
                Slog.Info($"{this}: no receiver: postpone {span.Length} bytes");
                _receivePostponed.Add(span);
            }
        }
        public virtual void Disconnected(ITpLink link)
        {
            if (_receiver != null)
                _receiver.Disconnected(this);
            else
            {
                Slog.Info($"{this}: no receiver: postpone disconnected");
                _receivePostponed.Disconnect();
            }
        }
    }
}