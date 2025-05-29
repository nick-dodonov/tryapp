using System.Collections.Generic;
using Common.Data;
using Cysharp.Text;
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

            var historyCycleRt = _timeContext.HistorySessionCycledRt;
            _history.VisitCycleKeyBounds((0, historyCycleRt),
                //TODO: visitor with state for static delegate
                (StKey key, ref StHistory<ServerState>.Item from, ref StHistory<ServerState>.Item to) =>
                {
                    var time = key.CycledRt;
                    var prevTime = from.Key.CycledRt;
                    var nextTime = to.Key.CycledRt;
                    var interval = (ushort)(nextTime - prevTime);
                    var diff = (ushort)(time - prevTime);

                    var t = interval > 0 ? Mathf.Clamp01((float)diff / interval) : 0;
                    //Shared.Log.Slog.Info($"FR={Time.frameCount}: {time,5}/[{prevTime,5} {nextTime,5}]: {diff,4}/{interval}={t:F3} - {DebugGetHistoryKeysArrayString()}");

                    _serverStateTweener.Process(ref _interpolatedState, t, in from.Value, in to.Value);
                });

            foreach (var peerState in _interpolatedState.Peers)
            {
                var peerId = peerState.Id;
                if (_peerViews.TryGetValue(peerId, out var peerView)) 
                    peerView.ApplyInterpolatedState(peerState);
            }
        }

        private string DebugGetHistoryKeysArrayString()
        {
            var sb = ZString.CreateStringBuilder(true);
            try
            {
                sb.Append('[');
                var idx = 0;
                foreach (ref var item in _history.ReverseRefItems)
                {
                    if (idx++ > 0)
                        sb.Append(", ");
                    sb.Append(item.Key.CycledRt);
                }
                sb.Append(']');
                return sb.ToString();
            }
            finally
            {
                sb.Dispose();
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