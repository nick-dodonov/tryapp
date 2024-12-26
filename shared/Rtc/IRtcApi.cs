using System;
using System.Threading;
using System.Threading.Tasks;

namespace Shared.Rtc
{
    public interface IRtcLink : IDisposable
    {
        public delegate void ReceivedCallback(byte[] bytes); //null - disconnected
        void Send(byte[] bytes);
    }

    public interface IRtcApi
    {
        Task<IRtcLink> Connect(IRtcLink.ReceivedCallback receivedCallback, CancellationToken cancellationToken);
    }
}