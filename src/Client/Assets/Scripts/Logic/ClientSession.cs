using System;
using System.Buffers;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Common.Logic;
using Common.Meta;
using Shared.Log;
using Shared.Tp;
using Shared.Tp.Rtc;
using Shared.Web;
using UnityEngine;

namespace Client.Logic
{
    /// <summary>
    /// Custom logic stub that begin/finish session and send/recieve data
    /// </summary>
    public class ClientSession : MonoBehaviour, ITpReceiver
    {
        private static readonly Slog.Area _log = new();

        public ClientTap clientTap;
        public GameObject peerPrefab;

        private IWebClient _webClient;
        private IMeta _meta;
        private ITpApi _tpApi;
        private ITpLink _tpLink;

        private readonly Dictionary<string, PeerTap> _peerTaps = new();

        private void OnEnable()
        {
            clientTap.SetActive(false);
        }

        private Action<string> _notifyFinishingCallback;

        public async Task Begin(
            Func<IWebClient> webClientFactory,
            Action<string> notifyFinishingCallback,
            CancellationToken cancellationToken)
        {
            _log.Info(".");
            if (_tpLink != null)
                throw new InvalidOperationException("RtcStart: link is already established");

            _notifyFinishingCallback = notifyFinishingCallback;

            _webClient = webClientFactory();
            _meta = new MetaClient(_webClient, Slog.Factory);
            _tpApi = RtcApiFactory.CreateApi(_meta.RtcService);
            //var localPeerId = GetLocalPeerId();
            _tpLink = await _tpApi.Connect(this, cancellationToken);
            _updateSendFrame = 0;

            clientTap.SetActive(true);

            // static string GetLocalPeerId()
            // {
            //     var peerId = SystemInfo.deviceUniqueIdentifier;
            //     if (peerId == SystemInfo.unsupportedIdentifier)
            //     {
            //         //TODO: reimplement using IPeerIdProvider
            //         peerId = Guid.NewGuid().ToString();
            //     }
            //     return peerId;
            // }
        }

        public void Finish(string reason)
        {
            _log.Info(reason);

            foreach (var kv in _peerTaps)
                Destroy(kv.Value.gameObject);
            _peerTaps.Clear();
            clientTap.SetActive(false);

            _tpLink?.Dispose();
            _tpLink = null;
            _tpApi = null;

            _meta?.Dispose();
            _meta = null;
            _webClient?.Dispose();
            _webClient = null;
        }

        private const float UpdateSendSeconds = 1.0f;
        private float _updateElapsedTime;
        private int _updateSendFrame;

        private void Update()
        {
            if (_tpLink == null)
                return;

            _updateElapsedTime += Time.deltaTime;
            if (_updateElapsedTime > UpdateSendSeconds)
            {
                _updateElapsedTime = 0;
                var clientState = GetClientState(_updateSendFrame++);
                Send(clientState);
            }
        }

        private ClientState GetClientState(int frame)
        {
            var utcMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var clientState = new ClientState
            {
                Frame = frame,
                UtcMs = utcMs
            };
            clientTap.Fill(ref clientState);
            return clientState;
        }

        private void Send(in ClientState clientState)
        {
            var msg = WebSerializer.SerializeObject(clientState);
            var bytes = System.Text.Encoding.UTF8.GetBytes(msg);
            _log.Info($"[{bytes.Length}] bytes: {msg}");
            _tpLink.Send(bytes);
        }

        void ITpReceiver.Received(ITpLink link, byte[] bytes)
        {
            if (bytes == null)
            {
                _log.Info("disconnected (notifying handler)");
                _notifyFinishingCallback("disconnected");
                return;
            }

            var msg = System.Text.Encoding.UTF8.GetString(bytes);
            _log.Info($"[{bytes.Length}] bytes: {msg}");

            try
            {
                var serverState = WebSerializer.DeserializeObject<ServerState>(msg);

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

                        peerTap.Apply(peerState.ClientState);
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
    }
}