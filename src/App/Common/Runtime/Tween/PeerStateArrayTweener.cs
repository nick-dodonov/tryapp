using System;
using Common.Data;
using Shared.Tp.Tween;

namespace Common.Tween
{
    public class PeerStateArrayTweener : ITweener<PeerState[]>
    {
        private readonly ITweener<PeerState> _peerTweener;
        public PeerStateArrayTweener(TweenerProvider provider)
        {
            _peerTweener = provider.Get<PeerState>();
        }

        public void Process(ref PeerState[] a, ref PeerState[] b, float t, ref PeerState[] r)
        {
            var length = b.Length;
            Array.Resize(ref r, length);
            for (var i = 0; i < length; ++i)
            {
                ref var rPeer = ref r[i];
                ref var bPeer = ref b[i];
                if (TryGetPeerStateIndex(a, bPeer.Id, out var fromPeerIndex))
                {
                    ref var aPeer = ref a[fromPeerIndex];
                    _peerTweener.Process(ref aPeer, ref bPeer, t, ref rPeer);
                }
                else
                    rPeer = bPeer;
            }
        }

        //TODO: cache Peers by Id in ServerState
        private static bool TryGetPeerStateIndex(in PeerState[] peers, string peerId, out int peerIndex)
        {
            peerIndex = -1;
            var length = peers.Length;
            for (var i = 0; i < length; ++i)
            {
                if (peers[i].Id != peerId)
                    continue;

                peerIndex = i;
                return true;
            }

            return false;
        }
    }
}