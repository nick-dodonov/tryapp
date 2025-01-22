using System;
using System.Buffers;
using System.Threading;
using System.Threading.Tasks;

//TODO: speedup modifying receiver/link signature to use GC-free variants of ReadOnlySequence<byte> and IBufferWriter<byte>

namespace Shared.Tp
{
    /// <summary>
    /// Interface for custom user logic of handling received data on client/server side
    /// </summary>
    public interface ITpReceiver
    {
        /// <param name="link">channel data from, allows to simplify server code allowing to use the same handler for several links</param>
        /// <param name="span">bytes block from link</param>
        /// TO-DO: possibly replace with ReadOnlySequence for links that merge data
        void Received(ITpLink link, ReadOnlySpan<byte> span);
        /// <param name="link">disconnected channel</param>
        void Disconnected(ITpLink link);
    }

    /// <summary>
    /// Interface for custom user logic of handling new connection on server side
    /// </summary>
    public interface ITpListener
    {
        /// <summary>
        /// Event for obtaining new link of established connection and setup receiving data handler  
        /// </summary>
        /// <param name="link">channel to send bytes</param>
        /// <returns>handler for receiving bytes, null to close connection (starting/terminating/filled server, etc.)</returns>
        public ITpReceiver? Connected(ITpLink link);
    }

    /// <summary>
    /// Delegate for sending data via buffer writer
    ///     (allows to use implementation buffer directly in user code and extensions)
    /// </summary>
    public delegate void TpWriteCb<in T>(IBufferWriter<byte> writer, T state);

    /// <summary>
    /// Interface for specific link implementations allowing to send data on client/server side
    /// </summary>
    public interface ITpLink : IDisposable
    {
        //TODO: replace by LinkId { get; } and
        //  think to replace to abstract type (maybe EndPoint) type instead allowing to keep extended info
        string GetRemotePeerId();

        /// <summary>
        /// Helper for user logic to obtain wrapped link layers (efficiently can be used once on Connected event)
        /// </summary>
        T? Find<T>() where T : ITpLink => this is T link ? link : default;

        /// <summary>
        /// Send via callback allows to write directly to implementation buffer even with using wrappers
        /// Note: don't forget to get rid of unnecessary closure allocation bypassing static delegate and state
        /// </summary>
        void Send<T>(TpWriteCb<T> writeCb, in T state);
    }

    /// <summary>
    /// Entry point interface of specific implementation of links (client/server side)
    /// </summary>
    public interface ITpApi
    {
        /// <summary>
        /// Helper for user logic to obtain wrapped link layers (efficiently can be used once on constructor)
        /// </summary>
        T? Find<T>() where T : ITpApi => this is T api ? api : default;
        
        /// <summary>
        /// Client side
        /// TODO: replace localPeerId with IPeerIdProvider passed to *Api impl
        /// </summary>
        ValueTask<ITpLink> Connect(ITpReceiver receiver, CancellationToken cancellationToken);

        /// <summary>
        /// Server side 
        /// </summary>
        void Listen(ITpListener listener);
    }
}