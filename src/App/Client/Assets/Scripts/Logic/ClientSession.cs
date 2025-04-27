using System;
using System.Buffers;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Client.UI;
using Common.Data;
using Common.Logic;
using Common.Meta;
using Cysharp.Text;
using Diagnostics.Debug;
using Shared.Log;
using Shared.Options;
using Shared.Tp;
using Shared.Tp.Data;
using Shared.Tp.Ext.Hand;
using Shared.Tp.Ext.Misc;
using Shared.Tp.Rtc;
using Shared.Tp.St.Sync;
using Shared.Web;
using UnityEngine;

namespace Client.Logic
{
    public interface ISessionWorkflowOperator
    {
        void Disconnected();
    }

    /// <summary>
    /// Custom logic stub that begin/finish session and send/recieve data
    /// </summary>
    public class ClientSession : MonoBehaviour, ISyncHandler<ClientState, ServerState>
    {
        private static readonly Slog.Area _log = new();

        public DebugControl debugControl;
        public InfoControl infoControl;

        public ClientTap clientTap;
        public GameObject peerPrefab;

        public ClientContext context;

        private IMeta _meta;
        private ITpApi _api;

        private StateSyncer<ClientState, ServerState> _stateSyncer;
        private TimeLink _timeLink; //cached
        private DumpLink _dumpLink; //cached

        private readonly Dictionary<string, PeerTap> _peerTaps = new();

        private void OnEnable()
        {
            clientTap.SetActive(false);
            RuntimePanel.SetInspectorContext(context);
        }

        private ISessionWorkflowOperator _workflowOperator;

        public async Task Begin(
            IWebClient webClient,
            ISessionWorkflowOperator workflowOperator,
            CancellationToken cancellationToken)
        {
            _log.Info(".");
            if (_stateSyncer != null)
                throw new InvalidOperationException("Link is already established");

            _workflowOperator = workflowOperator;

            // initialize for connection
            _meta = new MetaClient(webClient, Slog.Factory);
            var peerId = GetPeerId();
            _api = CommonSession.CreateApi<ClientConnectState, ServerConnectState>(
                RtcApiFactory.CreateApi(_meta.RtcService),
                new(peerId),
                (_) => $"{peerId}",
                new StaticOptionsMonitor<DumpLink.Options>(context.dumpLinkOptions),
                Slog.Factory
            );

            // connect to server
            _stateSyncer = await StateSyncerFactory.CreateAndConnect(this, _api, cancellationToken);

            // link diagnostics
            var link = _stateSyncer.Link;
            var handLink = link.Find<HandLink<ServerConnectState>>() ?? throw new("HandLink not found");
            debugControl.SetServerVersion(handLink.RemoteState.BuildVersion);

            _timeLink = link.Find<TimeLink>() ?? throw new("TimeLink not found");
            _dumpLink = link.Find<DumpLink>() ?? throw new("DumpLink not found");
            context.dumpLinkStats = _dumpLink.Stats;

            // enable player input
            clientTap.SetActive(true);
        }

        public void Finish(string reason)
        {
            _log.Info(reason);

            debugControl.SetServerVersion(null);

            foreach (var kv in _peerTaps)
                Destroy(kv.Value.gameObject);
            _peerTaps.Clear();

            clientTap.SetActive(false);

            _dumpLink = null;
            _timeLink = null;

            _stateSyncer?.Dispose();
            _stateSyncer = null;

            _api = null;
            _meta?.Dispose();
            _meta = null;
        }

        private void Update()
        {
            if (_stateSyncer == null)
                return;

            UpdateInfoControl();

            _stateSyncer.LocalUpdate(Time.deltaTime);
            foreach (var kv in _peerTaps)
                kv.Value.UpdateSessionMs(_timeLink.RemoteMs);
        }

        private void UpdateInfoControl()
        {
            var stats = _dumpLink.Stats.UpdateRates();
            var sb = ZString.CreateStringBuilder(true);
            try
            {
                sb.Append("session-sec: ");
                sb.Append(_timeLink.RemoteMs / 1000.0f, "F1");
                sb.AppendLine(" sec");

                sb.Append("out: ");
                sb.AppendStatDir(stats.Out);
                sb.AppendLine();

                sb.Append("in: ");
                sb.AppendStatDir(stats.In);
                sb.AppendLine();

                sb.Append("rtt: ");
                sb.Append(_timeLink.RttMs, "00");
                sb.AppendLine(" ms");

                infoControl.SetText(sb.ToString());
            }
            finally
            {
                sb.Dispose();
            }
        }

        SyncOptions ISyncHandler<ClientState, ServerState>.Options => context.syncOptions;
        IObjWriter<ClientState> ISyncHandler<ClientState, ServerState>.LocalWriter { get; } = TickStateFactory.CreateObjWriter<ClientState>();
        IObjReader<ServerState> ISyncHandler<ClientState, ServerState>.RemoteReader { get; } = TickStateFactory.CreateObjReader<ServerState>();

        ClientState ISyncHandler<ClientState, ServerState>.MakeLocalState(int sendIndex)
        {
            var sessionMs = _timeLink.RemoteMs;
            var clientState = new ClientState
            {
                Frame = sendIndex,
                Ms = sessionMs
            };
            clientTap.Fill(ref clientState);
            return clientState;
        }

        void ISyncHandler<ClientState, ServerState>.RemoteUpdated(ServerState serverState)
        {
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

                    peerTap.Apply(peerState);
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

        void ISyncHandler<ClientState, ServerState>.RemoteDisconnected()
        {
            _log.Info("notifying handler");
            _workflowOperator.Disconnected();
        }

        //TODO: reimplement using IPeerIdProvider for sign-in features
        private static string GetPeerId()
        {
            var peerId = SystemInfo.deviceUniqueIdentifier;
            if (peerId == SystemInfo.unsupportedIdentifier)
            {
                //TODO: implement for webgl platform (it doesn't support device unique id)
                peerId = Guid.NewGuid().ToString("N")[..8].ToUpper();
            }
            else
                peerId = peerId[..8].ToUpper(); //tmp short to simplify diagnostics
            return peerId;
        }
    }

    public static class DumpStatsExtensions
    {
        public static void AppendStatDir(this ref Utf16ValueStringBuilder sb, in DumpStats.Dir dir)
        {
            var bytesRate = dir.BytesRate;
            sb.Append(bytesRate);

            var countRate = dir.CountRate;
            if (countRate > 0)
            {
                sb.Append(" (");
                sb.Append(bytesRate / countRate);
                sb.Append(" * ");
                sb.Append(countRate);
                sb.Append(")");
            }

            sb.Append(" b/sec");
        }
    }
}