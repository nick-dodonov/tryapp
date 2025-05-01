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
        private readonly Dictionary<string, PeerView> _peerViews = new();

        public void Init(TimeLink timeLink)
        {
            _timeLink = timeLink;
        }

        private void OnDisable()
        {
            foreach (var (_, peerView) in _peerViews)
                Destroy(peerView.gameObject);
            _peerViews.Clear();
        }

        private void Update()
        {
            var sessionMs = _timeLink.RemoteMs;
            foreach (var (_, peerView) in _peerViews)
                peerView.UpdateSessionMs(sessionMs);
        }

        public void RemoteUpdated(ServerState serverState)
        {
            var sessionMs = _timeLink.RemoteMs;

            var count = 0;
            var pool = SlimMemoryPool<KeyValuePair<string, PeerView>>.Shared;
            using var owner = pool.Rent(_peerViews.Count);
            var span = owner.Memory.Span;

            foreach (var kv in _peerViews)
            {
                span[count++] = kv;
                kv.Value.SetChanged(false);
            }

            foreach (var peerState in serverState.Peers)
            {
                var peerId = peerState.Id;
                if (!_peerViews.TryGetValue(peerId, out var peerView))
                {
                    var peerGameObject = Instantiate(peerPrefab, transform);
                    peerView = peerGameObject.GetComponent<PeerView>();
                    _peerViews.Add(peerId, peerView);
                }

                peerView.ApplyState(peerState);
                peerView.UpdateSessionMs(sessionMs);
            }

            //remove peer views that don't exist anymore
            foreach (var (id, peerView) in span[..count])
            {
                if (peerView.Changed)
                    continue;
                _peerViews.Remove(id);
                Destroy(peerView.gameObject);
            }
        }
    }
}