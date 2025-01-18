using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Client.UI;
using Client.Utility;
using Common.Logic;
using Common.Meta;
using Diagnostics;
using Shared.Audit;
using Shared.Log;
using Shared.Tp;
using Shared.Tp.Rtc;
using Shared.Web;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Client
{
    public class HudLogic : MonoBehaviour, ISessionController, ITpReceiver
    {
        private static readonly Slog.Area _log = new();
    
        public ClientTap clientTap;
        public GameObject peerPrefab;
    
        public TMP_Text versionText;

        public TMP_Dropdown serverDropdown;
        public Button serverRequestButton;
        public TMP_Text serverResponseText;

        public SessionControl sessionControl;

        private record ServerOption(string Text, string Url);
        private readonly List<ServerOption> _serverOptions = new();

        private IWebClient _webClient;
        private IMeta _meta;
        private ITpApi _tpApi;
        private ITpLink _tpLink;

        private readonly Dictionary<string, PeerTap> _peerTaps = new();
    
        // ReSharper disable once AsyncVoidMethod
        private async void OnEnable()
        {
            //await UniTask.Delay(1000).WithCancellation(destroyCancellationToken);
            _log.Info("==== starting client (static) ====");
            // var logger = Slog.Factory.CreateLogger<HudLogic>();
            // logger.Info("==== starting client (logger) ====");
        
            StartupInfo.Print();
            versionText.text = $"Version: {Application.version}";

            if (NeedServerLocalhostOptions(out var localhost))
            {
                _log.Info("add localhost servers");
                _serverOptions.Add(new("localhost-debug", $"http://{localhost}:5270"));
                _serverOptions.Add(new("localhost-http", $"http://{localhost}"));
                _serverOptions.Add(new("localhost-ssl", $"https://{localhost}"));
            }
            var hostingOption = await NeedServerHostingOption();
            if (hostingOption != null)
            {
                _log.Info($"add server ({hostingOption.OriginDescription}): {hostingOption.Url}");
                _serverOptions.Add(new(hostingOption.OriginDescription, hostingOption.Url));
            }
            serverDropdown.options.Clear();
            serverDropdown.options.AddRange(
                _serverOptions.Select(x => new TMP_Dropdown.OptionData(x.Text)));
            serverDropdown.RefreshShownValue();
            serverResponseText.text = "";
        
            serverRequestButton.onClick.AddListener(OnServerRequestButtonClick);

            sessionControl.Controller = this;
        
            clientTap.SetActive(false);
        }

        private void OnDisable()
        {
            RtcStop("closing");
            serverRequestButton.onClick.RemoveListener(OnServerRequestButtonClick);
        }

        private const float UpdateSendSeconds = 1.0f;
        private float _updateElapsedTime;
        private int _updateSendFrame;
        private void Update()
        {
            //stub update/send logic
            if (_tpLink != null)
            {
                _updateElapsedTime += Time.deltaTime;
                if (_updateElapsedTime > UpdateSendSeconds)
                {
                    _updateElapsedTime = 0;
                
                    var utcMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    var clientState = new ClientState
                    {
                        Frame = _updateSendFrame++,
                        UtcMs = utcMs
                    };
                    clientTap.Fill(ref clientState);
                    var msg = WebSerializer.SerializeObject(clientState);
                
                    RtcSend(msg);
                }
            }
        }

        private static bool NeedServerLocalhostOptions(out string localhost)
        {
            localhost = "localhost";
            var absoluteURL = Application.absoluteURL;
            if (string.IsNullOrEmpty(absoluteURL))
                return true; // running in editor
            if (absoluteURL.Contains("localhost"))
                return true;
            localhost = "127.0.0.1";
            if (absoluteURL.Contains(localhost))
                return true;
            return false;
        }

        private record ServerOptions(string Url, string OriginDescription);
        private static async ValueTask<ServerOptions> NeedServerHostingOption()
        {
            if (NeedServerLocalhostOptions(out _))
            {
#if UNITY_EDITOR
                var env = OptionsReader.ParseEnvFileToDictionary();
                if (env.TryGetValue("SERVER_URL", out var envUrl))
                    return new(envUrl, ".env");
#endif
                var optionsUrl = await OptionsReader.TryParseOptionsJsonServerFirst();
                if (optionsUrl != null)
                    return new(new Uri(optionsUrl).GetLeftPart(UriPartial.Authority), "options.json");
            }

            var url = Application.absoluteURL;
            if (!string.IsNullOrEmpty(url))
            {
                url = new Uri(url).GetLeftPart(UriPartial.Authority);
                return new(url, "hosting");
            }

            return null;
        }

        private async void OnServerRequestButtonClick()
        {
            try
            {
                serverResponseText.text = "Requesting...";
                using var webClient = CreateWebClient();
                using var meta = CreateMetaClient(webClient);
                var result = await meta.GetInfo(destroyCancellationToken);
                serverResponseText.text = @$"Response:
\tRandomName: {result.RandomName}
\tRequestId: {result.RequestId}
\tRequestTime: {result.RequestTime:O}
";
            }
            catch (Exception ex)
            {
                serverResponseText.text = $"ERROR:\n{ex.Message}";
            }
        }

        Task ISessionController.StartSession(CancellationToken cancellationToken) => RtcStart(cancellationToken);
        void ISessionController.StopSession() => RtcStop();

        private async Task RtcStart(CancellationToken cancellationToken)
        {
            try
            {
                _log.Info(".");
                if (_tpLink != null)
                    throw new InvalidOperationException("RtcStart: link is already established");
            
                serverResponseText.text = "Requesting...";
                _webClient = CreateWebClient();
                _meta = CreateMetaClient(_webClient);
                _tpApi = RtcApiFactory.CreateApi(_meta.RtcService);
                //var localPeerId = GetLocalPeerId();
                _tpLink = await _tpApi.Connect(this, cancellationToken);
                _updateSendFrame = 0;

                clientTap.SetActive(true);
            
                serverResponseText.text = "RESULT:\nOK";
            }
            catch (Exception ex)
            {
                serverResponseText.text = $"ERROR:\n{ex.Message}";
                RtcStop("connect error");
                throw;
            }
        }

        private void RtcStop() => RtcStop("user request");
        private void RtcStop(string reason)
        {
            _log.Info(reason);
            sessionControl.NotifyStopped();

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

        private void RtcSend(string message)
        {
            _log.Info(message);
            var bytes = System.Text.Encoding.UTF8.GetBytes(message);
            _tpLink.Send(bytes);
        }

        void ITpReceiver.Received(ITpLink link, byte[] bytes) => RtcReceived(bytes);
        private void RtcReceived(byte[] data)
        {
            if (data == null)
            {
                RtcStop("disconnected");
                return;
            }
            var str = System.Text.Encoding.UTF8.GetString(data);
            _log.Info(str);
        
            try
            {
                var serverState = WebSerializer.DeserializeObject<ServerState>(str);

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
                
                    //remove peer taps that don't exist
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

        private IWebClient CreateWebClient()
        {
            var url = _serverOptions[serverDropdown.value].Url;
            return new UnityWebClient(url);
        }

        private static IMeta CreateMetaClient(IWebClient webClient) 
            => new MetaClient(webClient, Slog.Factory);
        
        private static string GetLocalPeerId()
        {
            var peerId = SystemInfo.deviceUniqueIdentifier;
            if (peerId == SystemInfo.unsupportedIdentifier)
            {
                //TODO: reimplement using IPeerIdProvider
                peerId = Guid.NewGuid().ToString();
            }
            return peerId;
        }
    }
}
