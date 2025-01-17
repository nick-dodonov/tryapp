using System;
using System.Threading;
using System.Threading.Tasks;

namespace Shared.Tp
{
    /// <summary>
    /// Interface for custom user logic of handling received data on client/server side
    /// </summary>
    public interface ITpReceiver
    {
        /// <summary>
        /// <param name="link">channel data from, allows to simplify server code allowing to use the same handler for several links</param>
        /// <param name="bytes">data block from link, null means disconnected</param> 
        /// </summary>
        void Received(ITpLink link, byte[]? bytes);
    }

    /// <summary>
    /// Interface for custom user logic of handling new connection on server side
    /// </summary>
    public interface ITpListener
    {
        public ITpReceiver Connected(ITpLink link);
    }

    /// <summary>
    /// Interface for specific link implementations allowing to send data on client/server side
    /// </summary>
    public interface ITpLink : IDisposable
    {
        void Send(byte[] bytes);
    }

    /// <summary>
    /// Entry point interface of specific implementation of links (client/server side)
    /// </summary>
    public interface ITpApi
    {
        /// <summary>
        /// Client side
        /// </summary>
        Task<ITpLink> Connect(ITpReceiver receiver, CancellationToken cancellationToken);

        /// <summary>
        /// Server side 
        /// </summary>
        void Listen(ITpListener listener);
    }
}