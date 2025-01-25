using System;
using System.Buffers;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Client.UI;
using Common.Logic;
using Common.Meta;
using Diagnostics.Debug;
using RuntimeInspectorNamespace;
using Shared.Log;
using Shared.Options;
using Shared.Tp;
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
    public class ClientSession : MonoBehaviour, ITpReceiver
    {
        private static readonly Slog.Area _log = new();

        public InfoControl infoControl;
        public ClientTap clientTap;
        public GameObject peerPrefab;

        public ClientContext context;

        private IMeta _meta;
        private ITpApi _api;

        private ITpLink _link;
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
            if (_link != null)
                throw new InvalidOperationException("RtcStart: link is already established");

            _workflowOperator = workflowOperator;

            _meta = new MetaClient(webClient, Slog.Factory);
            _api = CommonSession.CreateApi(
                RtcApiFactory.CreateApi(_meta.RtcService),
                new(GetPeerId()),
                new StaticOptionsMonitor<DumpLink.Options>(context.dumpLinkOptions),
                Slog.Factory
            );

            _link = await _api.Connect(this, cancellationToken);
            _timeLink = _link.Find<TimeLink>() ?? throw new("TimeLink not found");
            _dumpLink = _link.Find<DumpLink>() ?? throw new("DumpLink not found");
            context.dumpLinkStats = _dumpLink.Stats;

            _updateSendFrame = 0;

            clientTap.SetActive(true);
        }

        public void Finish(string reason)
        {
            _log.Info(reason);

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
        }

        private const float UpdateSendSeconds = 1.0f;
        private float _updateElapsedTime;
        private int _updateSendFrame;

        private void Update()
        {
            if (_link == null)
                return;

            _updateElapsedTime += Time.deltaTime;
            if (_updateElapsedTime > UpdateSendSeconds)
            {
                _updateElapsedTime = 0;
                var clientState = GetClientState(_updateSendFrame++);
                Send(clientState);
            }

            var sessionMs = _timeLink.RemoteMs;
            infoControl.SetText($"session: {sessionMs / 1000.0f:F1}sec ({_timeLink.RttMs}ms rtt)");
            foreach (var kv in _peerTaps)
                kv.Value.UpdateSessionMs(sessionMs);
        }

        private ClientState GetClientState(int frame)
        {
            var sessionMs = _timeLink.RemoteMs;
            var clientState = new ClientState
            {
                Frame = frame,
                Ms = sessionMs
            };
            clientTap.Fill(ref clientState);
            return clientState;
        }

        private void Send(in ClientState clientState)
        {
            _link.Send(WebSerializer.Default.Serialize, in clientState);
        }

        void ITpReceiver.Received(ITpLink link, ReadOnlySpan<byte> span)
        {
            try
            {
                var serverState = WebSerializer.Default.Deserialize<ServerState>(span);

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
            catch (Exception e)
            {
                _log.Error($"{e}");
            }
        }

        public void Disconnected(ITpLink link)
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