using System;
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

        public Player player;
        public PeersView peersView;

        public ClientContext context;

        private IMeta _meta;
        private ITpApi _api;

        private StSync<ClientState, ServerState> _stSync;
        private TimeLink _timeLink; //cached
        private DumpLink _dumpLink; //cached

        private void OnEnable()
        {
            peersView.gameObject.SetActive(false);
            player.gameObject.SetActive(false);
            RuntimePanel.SetInspectorContext(context);
        }

        private ISessionWorkflowOperator _workflowOperator;

        public async Task Begin(
            IWebClient webClient,
            ISessionWorkflowOperator workflowOperator,
            CancellationToken cancellationToken)
        {
            _log.Info(".");
            if (_stSync != null)
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
            _stSync = await StSyncFactory.CreateAndConnect(this, _api, cancellationToken);

            // link diagnostics
            var link = _stSync.Link;
            var handLink = link.Find<HandLink<ServerConnectState>>() ?? throw new("HandLink not found");
            debugControl.SetServerVersion(handLink.RemoteState.BuildVersion);

            _timeLink = link.Find<TimeLink>() ?? throw new("TimeLink not found");
            _dumpLink = link.Find<DumpLink>() ?? throw new("DumpLink not found");
            context.dumpLinkStats = _dumpLink.Stats;

            // enable peers view / player input
            peersView.Init(_timeLink);
            peersView.gameObject.SetActive(true);
            player.gameObject.SetActive(true); 
        }

        public void Finish(string reason)
        {
            _log.Info(reason);

            player.gameObject.SetActive(false);
            peersView.gameObject.SetActive(false);

            debugControl.SetServerVersion(null);
            
            _dumpLink = null;
            _timeLink = null;

            _stSync?.Dispose();
            _stSync = null;

            _api = null;
            _meta?.Dispose();
            _meta = null;
        }

        private void Update()
        {
            if (_stSync == null)
                return;

            UpdateInfoControl();

            _stSync.LocalUpdate(Time.deltaTime);
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

                infoControl.SetText(sb.AsArraySegment());
            }
            finally
            {
                sb.Dispose();
            }
        }

        SyncOptions ISyncHandler<ClientState, ServerState>.Options => context.syncOptions;
        IObjWriter<StCmd<ClientState>> ISyncHandler<ClientState, ServerState>.LocalWriter { get; } 
            = TickStateFactory.CreateObjWriter<StCmd<ClientState>>();
        IObjReader<StCmd<ServerState>> ISyncHandler<ClientState, ServerState>.RemoteReader { get; } 
            = TickStateFactory.CreateObjReader<StCmd<ServerState>>();

        ClientState ISyncHandler<ClientState, ServerState>.MakeLocalState()
        {
            var sessionMs = _timeLink.RemoteMs;
            var clientState = new ClientState
            {
                Ms = sessionMs
            };
            player.Fill(ref clientState);
            return clientState;
        }

        void ISyncHandler<ClientState, ServerState>.RemoteUpdated(ServerState serverState)
        {
            peersView.RemoteUpdated(serverState);
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