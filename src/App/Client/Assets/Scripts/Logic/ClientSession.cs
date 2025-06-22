using System;
using System.Threading;
using System.Threading.Tasks;
using Client.LocalSample;
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
using Shared.Tp.Rtc.Unity;
using Shared.Tp.St.Sync;
using Shared.Tp.Tick;
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

        public SampleObject sampleObject;
        public Player player;
        public ServerStateView serverStateView;

        public ClientContext context;

        private IMeta _meta;
        private ITpApi _api;

        private StSync<ClientState, ServerState> _stSync;
        private TimeLink _timeLink; //cached
        private DumpLink _dumpLink; //cached
        private ClientTimeContext _timeContext;

        private void OnEnable()
        {
            serverStateView.gameObject.SetActive(false);
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
                new StaticOptionsMonitor<TimeLink.Options>(context.timeLinkOptions),
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
            _timeContext = new(_timeLink, context.timeLinkOptions);

            // enable state view / player input
            serverStateView.Init(
                _timeContext, 
                _stSync.RemoteHistory, 
                CommonSession.CreateTweenerProvider());
            serverStateView.gameObject.SetActive(true);
            player.gameObject.SetActive(true); 
        }

        public void Finish(string reason)
        {
            if (_meta == null)
            {
                _log.Info($"skip: {reason}");
                return;
            }
            _log.Info(reason);

            if (player) // can be already destroyed
                player.gameObject.SetActive(false);
            if (serverStateView)
                serverStateView.gameObject.SetActive(false);

            debugControl.SetServerVersion(null);
            
            _dumpLink = null;
            _timeLink = null;

            _stSync?.Dispose();
            _stSync = null;

            _api = null;
            _meta?.Dispose();
            _meta = null;

            _log.Info("completed");
        }

        private void Update()
        {
            if (sampleObject)
                sampleObject.UpdateOptions(context.sampleOptions);

            if (_stSync == null)
                return;

            _timeContext.Update();
            UpdateInfoControl();

            _stSync.LocalUpdate(Time.deltaTime);
        }

        private void UpdateInfoControl()
        {
            var stats = _dumpLink.Stats.UpdateRates();
            var sb = ZString.CreateStringBuilder(true);
            try
            {
                sb.Append("local: ");
                var localTick = _timeLink.LocalClock.Tick();
                sb.Append(localTick.Seconds(), "F1");
                sb.Append(" - ");
                sb.Append(localTick.RtTick().Cycled().Seconds(), "F1");
                sb.AppendLine(" sec");

                sb.Append("remote: ");
                var remoteTickPoint = _timeLink.RemoteTicker.Point;
                sb.Append(remoteTickPoint.Seconds, "F1");
                sb.Append(" - ");
                sb.Append(remoteTickPoint.CycledSeconds, "F1");
                sb.AppendLine(" sec");

                sb.Append("st-hist: ");
                sb.AppendHistInfo(_stSync.LocalHistory);
                sb.Append(" (l) ");
                sb.AppendHistInfo(_stSync.RemoteHistory);
                sb.AppendLine(" (r) n/cap");

                sb.Append("out: ");
                sb.AppendStatDir(stats.Out);
                sb.AppendLine();

                sb.Append("in: ");
                sb.AppendStatDir(stats.In);
                sb.AppendLine();

                sb.Append($"rtt: ");
                ref var rttRtSet = ref _timeLink.RttRtSet;
                sb.AppendAligned(rttRtSet.Mean / Ticker.RtPerMs, "F1", 4);
                sb.Append(" ± ");
                sb.AppendAligned(rttRtSet.StdDeviation / Ticker.RtPerMs, "F1", 3);
                sb.Append("σ ");
                sb.AppendAligned(_timeLink.RttRt / (float)Ticker.RtPerMs, "F1", 5);
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

        ushort ISyncHandler<ClientState, ServerState>.CycledRt => _timeLink.RemoteTicker.Point.CycledRt;
        ClientState ISyncHandler<ClientState, ServerState>.MakeLocalState()
        {
            var clientState = new ClientState();
            player.Fill(ref clientState);
            return clientState;
        }

        void ISyncHandler<ClientState, ServerState>.RemoteUpdated() 
            => serverStateView.RemoteUpdated();

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
        private const char FigureSpace = '\u2007';

        public static void AppendAligned(this ref Utf16ValueStringBuilder sb, float value, string format, int width)
        {
            Span<char> buffer = stackalloc char[32];
            if (!value.TryFormat(buffer, out var charsWritten, format.AsSpan()))
            {
                Slog.Error($"failed: {value} ({format})");
                return;
            }

            var repeatCount = width - charsWritten;
            if (repeatCount > 0)
                sb.Append(FigureSpace, repeatCount);

            sb.Append(buffer[..charsWritten]);
        }
        
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

        public static void AppendHistInfo<T>(this ref Utf16ValueStringBuilder sb, in StHistory<T> history) 
        {
            sb.Append(history.Count);
            sb.Append('/');
            sb.Append(history.Capacity);
        }
    }
}