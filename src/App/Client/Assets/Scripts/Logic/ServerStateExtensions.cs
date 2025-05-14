using System;
using Common.Data;
using UnityEngine;

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

        public static void Interpolate(this ref ServerState state, in ServerState from, in ServerState to, float t)
        {
            var toPeers = to.Peers;

            Array.Resize(ref state.Peers, toPeers.Length);
            for (var i = 0; i < toPeers.Length; ++i)
            {
                ref var toPeer = ref toPeers[i];
                if (from.TryGetPeerStateIndex(toPeer.Id, out var fromPeerIndex))
                    state.Peers[i].Interpolate(from.Peers[fromPeerIndex], toPeer, t);
                else
                    state.Peers[i] = toPeer;
            }
        }

        private static void Interpolate(this ref PeerState state, in PeerState from, in PeerState to, float t)
        {
            state.Ms = to.Ms;
            state.Id = to.Id;
            state.ClientState.Interpolate(from.ClientState, to.ClientState, t);
        }
        
        private static void Interpolate(this ref ClientState state, in ClientState from, in ClientState to, float t)
        {
            state.X = Mathf.Lerp(from.X, to.X, t);;
            state.Y = Mathf.Lerp(from.Y, to.Y, t);;
            state.Color = to.Color;
        }
    }
}