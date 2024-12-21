using System;
using System.Collections.Generic;
using System.Linq;
using Diagnostics;
using Shared;
using Shared.Meta.Client;
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

    private record ServerOption(string Text, string Url);
    private readonly List<ServerOption> _serverOptions = new();

    private void OnEnable()
    {
        //await UniTask.Delay(1000).WithCancellation(destroyCancellationToken);
        StaticLog.Info("==== starting client ====");
        StartupInfo.Print();
        versionText.text = $"Version: {Application.version}";
        
        if (NeedServerLocalhostOptions())
        {
            _serverOptions.Add(new("localhost-debug", "http://localhost:5270"));
            _serverOptions.Add(new("localhost-http", "http://localhost"));
            _serverOptions.Add(new("localhost-ssl", "https://localhost"));
        }
        if (NeedServerHostingOption(out var url))
            _serverOptions.Add(new("hosting", url));
        serverDropdown.options.Clear();
        serverDropdown.options.AddRange(
            _serverOptions.Select(x => new TMP_Dropdown.OptionData(x.Text)));
        serverRequestButton.onClick.AddListener(OnServerRequestButtonClick);
        serverResponseText.text = "";
    }

    private static bool NeedServerLocalhostOptions()
    {
        var absoluteURL = Application.absoluteURL;
        if (string.IsNullOrEmpty(absoluteURL))
            return true; // running in editor
        if (absoluteURL.Contains("localhost"))
            return true;
        return false;
    }

    private bool NeedServerHostingOption(out string url)
    {
        url = Application.absoluteURL;
        if (!string.IsNullOrEmpty(url))
            url = new Uri(url).GetLeftPart(UriPartial.Authority);

#if UNITY_EDITOR
        if (NeedServerLocalhostOptions())
        {
            var env = ParseEnvFileToDictionary();
            if (env.TryGetValue("SERVER_URL", out url))
                StaticLog.Info($"server url: {url}");
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


    private void OnDisable()
    {
        serverRequestButton.onClick.RemoveListener(OnServerRequestButtonClick);
    }

    private async void OnServerRequestButtonClick()
    {
        try
        {
            serverResponseText.text = "Requesting...";
            var url = _serverOptions[serverDropdown.value].Url;
            using var client = new UnityWebClient(url);
            var meta = new MetaClient(client);
            var result = await meta.GetInfo(destroyCancellationToken);
            serverResponseText.text = $"Response:\n{result.RequestTime:O}";
        }
        catch (Exception ex)
        {
            serverResponseText.text = $"ERROR:\n{ex.Message}";
        }
    }
}
