using Common.Data;

namespace Client.Logic
{
    public static class ServerStateExtensions
    {
        //TODO: cache Peers by Id in ServerState
        public static bool TryGetPeerStateIndex(this in ServerState state, string peerId, out int peerIndex)
        {
            peerIndex = -1;

            var peers = state.Peers;
            for (var i = 0; i < peers.Length; ++i)
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