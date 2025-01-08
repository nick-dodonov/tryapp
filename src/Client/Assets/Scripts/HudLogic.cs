using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Diagnostics;
using Client.Rtc;
using Microsoft.Extensions.Logging;
using Shared.Log;
using Shared.Meta.Api;
using Shared.Meta.Client;
using Shared.Rtc;
using Shared.Session;
using Shared.Web;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HudLogic : MonoBehaviour
{
    private static readonly Slog.Area _log = new();
    
    public PlayerTap PlayerTap;
    
    public TMP_Text versionText;

    public TMP_Dropdown serverDropdown;
    public Button serverRequestButton;
    public TMP_Text serverResponseText;

    public Button rtcStartButton;
    public Button rtcStopButton;

    private record ServerOption(string Text, string Url);
    private readonly List<ServerOption> _serverOptions = new();

    private IMeta _meta;
    private IRtcApi _rtcApi;
    private IRtcLink _rtcLink;

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
        rtcStartButton.onClick.AddListener(RtcStart);
        rtcStopButton.onClick.AddListener(RtcStop);
    }

    private void OnDisable()
    {
        RtcStop("closing");
        rtcStopButton.onClick.AddListener(RtcStop);
        rtcStartButton.onClick.RemoveListener(RtcStart);
        serverRequestButton.onClick.RemoveListener(OnServerRequestButtonClick);
    }

    private const float UpdateSendSeconds = 1.0f;
    private float _updateElapsedTime;
    private int _updateSendFrame;
    private void Update()
    {
        //stub update/send logic
        if (_rtcLink != null)
        {
            _updateElapsedTime += Time.deltaTime;
            if (_updateElapsedTime > UpdateSendSeconds)
            {
                _updateElapsedTime = 0;
                
                var utcMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                //msg = $"{_updateSendFrame};TODO-FROM-CLIENT;{utcMs}";
                
                var clientState = new ClientState
                {
                    Frame = _updateSendFrame,
                    UtcMs = utcMs
                };
                PlayerTap.Fill(ref clientState);
                var msg = WebSerializer.SerializeObject(clientState);
                
                _updateSendFrame++;
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
            //TryBind.TestCallbacks(); //TODO: mv to debug console for testing
            serverResponseText.text = "Requesting...";
            using var meta = CreateMetaClient();
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
    
    private async void RtcStart()
    {
        try
        {
            _log.Info(".");
            if (_rtcLink != null)
                throw new InvalidOperationException("RtcStart: link is already established");
            
            serverResponseText.text = "Requesting...";
            _meta = CreateMetaClient();
            _rtcApi = RtcApiFactory.CreateRtcClient(_meta);
            _rtcLink = await _rtcApi.Connect(RtcReceived, destroyCancellationToken);
            _updateSendFrame = 0;
                
            serverResponseText.text = "RESULT:\nOK";
        }
        catch (Exception ex)
        {
            serverResponseText.text = $"ERROR:\n{ex.Message}";
            RtcStop("connect error");
        }
    }

    private void RtcStop() => RtcStop("user request");
    private void RtcStop(string reason)
    {
        _log.Info(reason);
        _rtcLink?.Dispose();
        _rtcLink = null;
        _rtcApi = null;
        _meta?.Dispose();
        _meta = null;
    }

    private void RtcSend(string message)
    {
        _log.Info(message);
        var bytes = System.Text.Encoding.UTF8.GetBytes(message);
        _rtcLink.Send(bytes);
    }

    private void RtcReceived(byte[] data)
    {
        if (data == null)
        {
            RtcStop("disconnected");
            return;
        }
        var str = System.Text.Encoding.UTF8.GetString(data);
        _log.Info(str);
    }

    private IMeta CreateMetaClient()
    {
        var url = _serverOptions[serverDropdown.value].Url;
        var meta = new MetaClient(new UnityWebClient(url), Slog.Factory.CreateLogger<MetaClient>());
        return meta;
    }
}
