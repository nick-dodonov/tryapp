using System.Collections.Generic;
using Common.Data;
using Shared.Tp.Ext.Misc;
using Shared.Tp.Util;
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
            var pool = SlimMemoryPool<KeyValuePair<string, PeerTap>>.Shared;
            using var owner = pool.Rent(_peerTaps.Count);
            var span = owner.Memory.Span;

            foreach (var kv in _peerTaps)
            {
                span[count++] = kv;
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

                peerTap.ApplyState(peerState);
                peerTap.UpdateSessionMs(sessionMs);
            }

            //remove peer taps that don't exist anymore
            foreach (var (id, peerTap) in span[..count])
            {
                if (peerTap.Changed)
                    continue;
                _peerTaps.Remove(id);
                Destroy(peerTap.gameObject);
            }
        }
    }
}