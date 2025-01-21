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
    /// Interface for specific link implementations allowing to send data on client/server side
    /// </summary>
    public interface ITpLink : IDisposable
    {
        string GetRemotePeerId();

        void Send(ReadOnlySpan<byte> span);
        //TRY void Send<T>(WriteCb<T> writeCb, in T state);
    }

    public delegate void TpWriteCb<in T>(IBufferWriter<byte> writer, T state);
    public static class TpLinkExtensions
    {
        //TRY solution for future
        public static void Send<T>(this ITpLink link, TpWriteCb<T> writeCb, in T state)
        {
            var writer = new ArrayBufferWriter<byte>(); //TODO: speedup: use pooled / cached writer
            writeCb.Invoke(writer, state);
            link.Send(writer.WrittenSpan);
        }
    }

    /// <summary>
    /// Entry point interface of specific implementation of links (client/server side)
    /// </summary>
    public interface ITpApi
    {
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