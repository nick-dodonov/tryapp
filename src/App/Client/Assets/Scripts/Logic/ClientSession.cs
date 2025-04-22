using System;
using System.Buffers;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Client.UI;
using Common.Logic;
using Common.Meta;
using Diagnostics.Debug;
using Microsoft.Extensions.Logging;
using Shared.Boot.Version;
using Shared.Log;
using Shared.Options;
using Shared.Tp;
using Shared.Tp.Ext.Hand;
using Shared.Tp.Ext.Misc;
using Shared.Tp.Rtc;
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
    public class ClientSession : MonoBehaviour, ITpReceiver, ISyncHandler<ClientState, ServerState>
    {
        private static readonly Slog.Area _log = new();

        public DebugControl debugControl;
        public InfoControl infoControl;

        public ClientTap clientTap;
        public GameObject peerPrefab;

        public ClientContext context;

        private IMeta _meta;
        private ITpApi _api;

        private ITpLink _link;
        private TimeLink _timeLink; //cached
        private DumpLink _dumpLink; //cached
        
        private StateSyncer<ClientState, ServerState> _stateSyncer;

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
            if (_link != null)
                throw new InvalidOperationException("RtcStart: link is already established");

            _workflowOperator = workflowOperator;

            _meta = new MetaClient(webClient, Slog.Factory);
            _api = CommonSession.CreateApi<ClientConnectState, ServerConnectState>(
                RtcApiFactory.CreateApi(_meta.RtcService),
                new(GetPeerId()),
                static (state) => state.PeerId,
                static (_) => "SRV",
                 new StaticOptionsMonitor<DumpLink.Options>(context.dumpLinkOptions),
                Slog.Factory
            );

            _link = await _api.Connect(this, cancellationToken);

            var handLink = _link.Find<HandLink<ClientConnectState, ServerConnectState>>() ?? throw new("HandLink not found");
            debugControl.SetServerVersion(handLink.RemoteState.BuildVersion);

            _timeLink = _link.Find<TimeLink>() ?? throw new("TimeLink not found");
            _dumpLink = _link.Find<DumpLink>() ?? throw new("DumpLink not found");

            context.dumpLinkStats = _dumpLink.Stats;

            var logger = Slog.Factory.CreateLogger<StateSyncer<ClientState, ServerState>>();
            _stateSyncer = new(this, _link, logger);

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
            _link?.Dispose();
            _link = null;

            _api = null;
            _meta?.Dispose();
            _meta = null;

            // destroy after link to not fail on latest Received
            _stateSyncer?.Dispose();
            _stateSyncer = null;
        }

        private void Update()
        {
            if (_link == null)
                return;

            var sessionMs = _timeLink.RemoteMs;
            {
                var stats = _dumpLink.Stats;
                stats.UpdateRates();
                infoControl.SetText(@$"session: 
time: {sessionMs / 1000.0f:F1}sec 
rtt: {_timeLink.RttMs}ms 
in/out: {stats.In.Rate}/{stats.Out.Rate} bytes/sec");
            }

            _stateSyncer.LocalUpdate(Time.deltaTime);

            foreach (var kv in _peerTaps)
                kv.Value.UpdateSessionMs(sessionMs);
        }

        SyncOptions ISyncHandler<ClientState, ServerState>.Options => context.syncOptions;

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

        void ISyncHandler<ClientState, ServerState>.ReceivedRemoteState(ServerState serverState)
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

        void ITpReceiver.Received(ITpLink link, ReadOnlySpan<byte> span) => 
            _stateSyncer.RemoteUpdate(span);

        void ITpReceiver.Disconnected(ITpLink link)
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
}