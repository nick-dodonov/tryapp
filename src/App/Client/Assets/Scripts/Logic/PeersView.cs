using System.Collections.Generic;
using Common.Data;
using Shared.Tp.Ext.Misc;
using Shared.Tp.St.Sync;
using Shared.Tp.Util;
using UnityEngine;

namespace Client.Logic
{
    //TODO: !!!!! REWORK INTERPOLATIONS POC !!!!!
    
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
        int IViewHandler.SessionMs => _frameSessionMs;

        private ServerState _interpolatedState;

        private void Update()
        {
            _frameSessionMs = _timeLink.RemoteMs;

            //TODO: currentMs using smoothed _timeLink.RttMs
            var currentMs = _frameSessionMs - 210; //TODO: XXXXXXXX constant based on current server's send rate 

            _history.VisitExistingBounds(
                (0, currentMs),
                //TODO: VisitExistingBounds with state for static delegate
                (StKey key, ref StHistory<ServerState>.Item from, ref StHistory<ServerState>.Item to) =>
                {
                    var interval = to.Key.Ms - from.Key.Ms;
                    var value = key.Ms - from.Key.Ms;
                    var t = interval > 0 ? Mathf.Clamp01((float)value / interval) : 0;
                    //Shared.Log.Slog.Info($"FRAME={Time.frameCount}: {_frameSessionMs}-{key.Ms}: [{from.Key.Ms} {to.Key.Ms}]: {value}/{interval}: {t}");
                    _interpolatedState.Interpolate(from.Value, to.Value, t);
                    
                    foreach (var peerState in _interpolatedState.Peers)
                    {
                        var peerId = peerState.Id;
                        if (_peerViews.TryGetValue(peerId, out var peerView)) 
                            peerView.ApplyInterpolatedState(peerState);
                    }
                });
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

                peerView.ApplyLastState(peerState, _history);
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