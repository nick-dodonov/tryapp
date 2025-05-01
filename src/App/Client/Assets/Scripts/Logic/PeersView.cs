using System;
using System.Buffers;
using System.Collections.Generic;
using Common.Data;
using Shared.Tp.Ext.Misc;
using UnityEngine;

namespace Client.Logic
{
    public class PeersView : MonoBehaviour
    {
        public GameObject peerPrefab;

        private TimeLink _timeLink;
        private readonly Dictionary<string, PeerTap> _peerTaps = new();

        public void Init(TimeLink timeLink)
        {
            _timeLink = timeLink;
        }

        private void OnDisable()
        {
            foreach (var (_, peerTap) in _peerTaps)
                Destroy(peerTap.gameObject);
            _peerTaps.Clear();
        }

        private void Update()
        {
            var sessionMs = _timeLink.RemoteMs;
            foreach (var (_, peerTap) in _peerTaps)
                peerTap.UpdateSessionMs(sessionMs);
        }

        public void RemoteUpdated(ServerState serverState)
        {
            var sessionMs = _timeLink.RemoteMs;

            var count = 0;
            var peerKvsPool = ArrayPool<KeyValuePair<string, PeerTap>>.Shared;
            var peerKvs = peerKvsPool.Rent(_peerTaps.Count);
            try
            {
                foreach (var kv in _peerTaps)
                {
                    peerKvs[count++] = kv;
                    kv.Value.SetChanged(false);
                }

                foreach (var peerState in serverState.Peers)
                {
                    var peerId = peerState.Id;
                    if (!_peerTaps.TryGetValue(peerId, out var peerTap))
                    {
                        var peerGameObject = Instantiate(peerPrefab, transform);
                        peerTap = peerGameObject.GetComponent<PeerTap>();
                        _peerTaps.Add(peerId, peerTap);
                    }

                    peerTap.Apply(peerState, sessionMs);
                }

                //remove peer taps that don't exist anymore
                foreach (var (id, peerTap) in peerKvs.AsSpan(0, count))
                {
                    if (peerTap.Changed)
                        continue;
                    _peerTaps.Remove(id);
                    Destroy(peerTap.gameObject);
                }
            }
            finally
            {
                peerKvsPool.Return(peerKvs);
            }
        }
    }
}