using System;
using System.Threading;
using System.Threading.Tasks;

namespace Shared.Rtc
{
    public interface IRtcLink : IDisposable
    {
        public delegate void ReceivedCallback(byte[]? bytes); //null - disconnected
        void Send(byte[] bytes);
    }

    public interface IRtcApi
    {
        public delegate IRtcLink.ReceivedCallback ConnectionCallback(IRtcLink link); //null - disconnected

        Task<IRtcLink> Connect(IRtcLink.ReceivedCallback receivedCallback, CancellationToken cancellationToken);
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