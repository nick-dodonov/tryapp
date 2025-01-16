using System;
using System.Threading;
using System.Threading.Tasks;

namespace Shared.Rtc
{
    /// <summary>
    /// Interface for custom user logic handling on client/server side
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
        /// Client side part of specific impl
        /// </summary>
        Task<IRtcLink> Connect(IRtcReceiver receiver, CancellationToken cancellationToken);

        public delegate IRtcReceiver ConnectionCallback(IRtcLink link); //null - disconnected
        void Listen(ConnectionCallback connectionCallback);
    }
    
    /// <summary>
    /// TODO: make different implementations (not only current REST variant but WebSocket too)
    /// </summary>
    public interface IRtcService
    {
        //TODO: shared RTC types for SDP (offer, answer) and ICE candidates
        public ValueTask<string> GetOffer(string id, CancellationToken cancellationToken);
        public ValueTask<string> SetAnswer(string id, string answer, CancellationToken cancellationToken);
        public ValueTask AddIceCandidates(string id, string candidates, CancellationToken cancellationToken);
    }
}