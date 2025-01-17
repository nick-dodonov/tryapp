using System;
using System.Threading;
using System.Threading.Tasks;

namespace Shared.Rtc
{
    /// <summary>
    /// Interface for custom user logic of handling received data on client/server side
    /// </summary>
    public interface IRtcReceiver
    {
        /// <summary>
        /// <param name="link">channel data from, allows to simplify server code allowing to use the same handler for several links</param>
        /// <param name="bytes">data block from link, null means disconnected</param> 
        /// </summary>
        void Received(IRtcLink link, byte[]? bytes);
    }

    /// <summary>
    /// Interface for custom user logic of handling new connection on server side
    /// </summary>
    public interface IRtcListener
    {
        public IRtcReceiver Connected(IRtcLink link);
    }

    /// <summary>
    /// Interface for specific link implementations allowing to send data on client/server side
    /// </summary>
    public interface IRtcLink : IDisposable
    {
        void Send(byte[] bytes);
    }

    /// <summary>
    /// Entry point interface of specific implementation of links (client/server side)
    /// </summary>
    public interface IRtcApi
    {
        /// <summary>
        /// Client side
        /// </summary>
        Task<IRtcLink> Connect(IRtcReceiver receiver, CancellationToken cancellationToken);

        /// <summary>
        /// Server side 
        /// </summary>
        void Listen(IRtcListener listener);
    }
}