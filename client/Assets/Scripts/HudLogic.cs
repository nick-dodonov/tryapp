using System;
using System.Collections.Generic;
using System.Linq;
using Diagnostics;
using Client.Rtc;
using Shared;
using Shared.Meta.Api;
using Shared.Meta.Client;
using Shared.Rtc;
using Shared.Web;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HudLogic : MonoBehaviour
{
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

    private void OnEnable()
    {
        //await UniTask.Delay(1000).WithCancellation(destroyCancellationToken);
        StaticLog.Info("HudLogic: ==== starting client ====");
        StartupInfo.Print();
        versionText.text = $"Version: {Application.version}";
        
        if (NeedServerLocalhostOptions(out var localhost))
        {
            _serverOptions.Add(new("localhost-debug", $"http://{localhost}:5270"));
            _serverOptions.Add(new("localhost-http", $"http://{localhost}"));
            _serverOptions.Add(new("localhost-ssl", $"https://{localhost}"));
        }
        if (NeedServerHostingOption(out var url))
            _serverOptions.Add(new("hosting", url));
        serverDropdown.options.Clear();
        serverDropdown.options.AddRange(
            _serverOptions.Select(x => new TMP_Dropdown.OptionData(x.Text)));
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

                var message = $"{_updateSendFrame++};TODO-FROM-CLIENT;{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
                StaticLog.Info($"HudLogic: RtcSend: {message}");
                var bytes = System.Text.Encoding.UTF8.GetBytes(message);
                _rtcLink.Send(bytes);
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

    private static bool NeedServerHostingOption(out string url)
    {
        url = Application.absoluteURL;
        if (!string.IsNullOrEmpty(url))
            url = new Uri(url).GetLeftPart(UriPartial.Authority);

#if UNITY_EDITOR
        if (NeedServerLocalhostOptions(out _))
        {
            var env = ParseEnvFileToDictionary();
            if (env.TryGetValue("SERVER_URL", out url))
                StaticLog.Info($"HudLogic: server url: {url}");
        }

        static Dictionary<string, string> ParseEnvFileToDictionary()
        {
            var directory = System.IO.Directory.GetCurrentDirectory();
            Dictionary<string, string> envVariables = new();

            while (!string.IsNullOrEmpty(directory))
            {
                var envFilePath = System.IO.Path.Combine(directory, ".env");
                if (System.IO.File.Exists(envFilePath))
                {
                    var lines = System.IO.File.ReadAllLines(envFilePath);
                    foreach (var line in lines)
                    {
                        var trimmedLine = line.Trim();
                        if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith("#"))
                            continue;

                        var splitIndex = trimmedLine.IndexOf('=');
                        if (splitIndex <= 0)
                            continue;
                    
                        var key = trimmedLine[..splitIndex].Trim();
                        var value = trimmedLine[(splitIndex + 1)..].Trim();
                        envVariables[key] = value;
                    }
                    break; // Stop searching once the .env file is found and processed
                }
                directory = System.IO.Path.GetDirectoryName(directory); // Move up one directory
            }

            return envVariables;
        }
#endif
        return true;
    }

    private async void OnServerRequestButtonClick()
    {
        try
        {
            TryBind.TestCallbacks(); //TODO: mv to debug console for testing

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
            StaticLog.Info("HudLogic: RtcStart");
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
        StaticLog.Info($"HudLogic: RtcStop: {reason}");
        _rtcLink?.Dispose();
        _rtcLink = null;
        _rtcApi = null;
        _meta?.Dispose();
        _meta = null;
    }

    private void RtcReceived(byte[] data)
    {
        if (data == null)
        {
            RtcStop("disconnected");
            return;
        }
        var str = System.Text.Encoding.UTF8.GetString(data);
        StaticLog.Info($"HudLogic: RtcReceived: {str}");
    }

    private IMeta CreateMetaClient()
    {
        var url = _serverOptions[serverDropdown.value].Url;
        var meta = new MetaClient(new UnityWebClient(url));
        return meta;
    }
}
