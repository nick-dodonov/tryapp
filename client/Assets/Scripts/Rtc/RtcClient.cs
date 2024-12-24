using System;
using System.Threading;
using System.Threading.Tasks;
using Shared.Meta.Api;
using Unity.WebRTC;

namespace Rtc
{
    public class RtcClient
    {
        private RTCPeerConnection _peerConnection;

        public async Task<string> TryCall(IMeta meta, CancellationToken cancellationToken)
        {
            var id = Guid.NewGuid().ToString();
            var result = await meta.GetOffer(id, cancellationToken);
            return result;
        }
    }
}