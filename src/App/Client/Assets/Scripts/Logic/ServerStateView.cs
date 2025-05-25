using System.Collections.Generic;
using Common.Data;
using Shared.Tp.St.Sync;
using Shared.Tp.Tween;
using Shared.Tp.Util;
using UnityEngine;

namespace Client.Logic
{
    public class ServerStateView : MonoBehaviour
    {
        public GameObject peerPrefab;

        private ITimeContext _timeContext;
        private StHistory<ServerState> _history;
        private ITweener<ServerState> _serverStateTweener;

        private readonly Dictionary<string, PeerView> _peerViews = new();

        public void Init(ITimeContext timeContext, StHistory<ServerState> serverHistory, TweenerProvider tweenerProvider)
        {
            _timeContext = timeContext;
            _history = serverHistory;
            _serverStateTweener = tweenerProvider.Get<ServerState>();
        }

        private void OnDisable()
        {
            foreach (var (_, peerView) in _peerViews)
                Destroy(peerView.gameObject);
            _peerViews.Clear();
        }

        private ServerState _interpolatedState;

        private void Update()
        {
            if (_history.Count <= 0)
                return;

            var historyMs = _timeContext.HistorySessionMs;

            _history.VisitExistingBounds(
                (0, historyMs),
                //TODO: VisitExistingBounds with state for static delegate
                (StKey key, ref StHistory<ServerState>.Item from, ref StHistory<ServerState>.Item to) =>
                {
                    var interval = to.Key.Ms - from.Key.Ms;
                    var value = key.Ms - from.Key.Ms;
                    var t = interval > 0 ? Mathf.Clamp01((float)value / interval) : 0;
                    //Shared.Log.Slog.Info($"FRAME={Time.frameCount}: {sessionMs}-{key.Ms}: [{from.Key.Ms} {to.Key.Ms}]: {value}/{interval}: {t}");
                    _serverStateTweener.Process(ref _interpolatedState, t, in from.Value, in to.Value);
                });

            foreach (var peerState in _interpolatedState.Peers)
            {
                var peerId = peerState.Id;
                if (_peerViews.TryGetValue(peerId, out var peerView)) 
                    peerView.ApplyInterpolatedState(peerState);
            }
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
                    peerView.SetViewHandler(_timeContext);
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