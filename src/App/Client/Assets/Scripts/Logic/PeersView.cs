using System.Collections.Generic;
using Common.Data;
using Shared.Tp.Ext.Misc;
using Shared.Tp.St.Sync;
using Shared.Tp.Util;
using UnityEngine;

namespace Client.Logic
{
    public class PeersView : MonoBehaviour, IViewHandler
    {
        public GameObject peerPrefab;

        private TimeLink _timeLink;
        private StHistory<ServerState> _history;

        private readonly Dictionary<string, PeerView> _peerViews = new();

        public void Init(TimeLink timeLink, StHistory<ServerState> serverHistory)
        {
            _timeLink = timeLink;
            _history = serverHistory;
        }

        private void OnDisable()
        {
            foreach (var (_, peerView) in _peerViews)
                Destroy(peerView.gameObject);
            _peerViews.Clear();
        }

        private int _frameSessionMs;
        public int SessionMs => _frameSessionMs;
        private void Update()
        {
            _frameSessionMs = _timeLink.RemoteMs;
        }

        public void RemoteUpdated()
        {
            var count = 0;
            var pool = SlimMemoryPool<KeyValuePair<string, PeerView>>.Shared;
            using var owner = pool.Rent(_peerViews.Count);
            var span = owner.Memory.Span;

            foreach (var kv in _peerViews)
            {
                span[count++] = kv;
                kv.Value.SetChanged(false);
            }

            ref var serverState = ref _history.LastValueRef;
            foreach (var peerState in serverState.Peers)
            {
                var peerId = peerState.Id;
                if (!_peerViews.TryGetValue(peerId, out var peerView))
                {
                    var peerGameObject = Instantiate(peerPrefab, transform);
                    peerView = peerGameObject.GetComponent<PeerView>();
                    peerView.SetViewHandler(this);
                    _peerViews.Add(peerId, peerView);
                }

                peerView.ApplyState(peerState, _history);
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