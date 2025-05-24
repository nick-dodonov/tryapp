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

        public void Replica(in PeerState[] src, ref PeerState[] dst)
        {
            var length = src.Length;
            Array.Resize(ref dst, length);
            for (var i = 0; i < length; ++i)
                _peerTweener.Replica(src[i], ref dst[i]);
        }

        public void Process(in PeerState[] src0, in PeerState[] src1, float t, ref PeerState[] dst)
        {
            var length = src1.Length;
            Array.Resize(ref dst, length);
            for (var i = 0; i < length; ++i)
            {
                ref var rPeer = ref dst[i];
                ref var bPeer = ref src1[i];
                if (TryGetPeerStateIndex(src0, bPeer.Id, out var idx0))
                {
                    ref var aPeer = ref src0[idx0];
                    _peerTweener.Process(in aPeer, in bPeer, t, ref rPeer);
                }
                else
                    _peerTweener.Replica(bPeer, ref rPeer);
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