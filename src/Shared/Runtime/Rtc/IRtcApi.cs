using System;
using System.Threading;
using System.Threading.Tasks;

namespace Shared.Rtc
{
    public interface IRtcLink : IDisposable
    {
        /// <summary>
        /// `link` param allows to simplify some code allowing to use the same handler for several links (for example on server) 
        /// </summary>
        public delegate void ReceivedCallback(IRtcLink link, byte[]? bytes); //null - disconnected
        void Send(byte[] bytes);
    }

    public interface IRtcApi
    {
        Task<IRtcLink> Connect(IRtcLink.ReceivedCallback receivedCallback, CancellationToken cancellationToken);

        public delegate IRtcLink.ReceivedCallback ConnectionCallback(IRtcLink link); //null - disconnected
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