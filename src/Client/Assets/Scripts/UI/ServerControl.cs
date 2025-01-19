using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Client.Utility;
using Common.Meta;
using Shared.Log;
using Shared.Web;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Client.UI
{
    public class ServerControl : MonoBehaviour
    {
        private static readonly Slog.Area _log = new();

        public InfoControl infoControl;

        public TMP_Dropdown serverDropdown;
        public Button serverRequestButton;

        private record ServerOption(string Text, string Url);

        private readonly List<ServerOption> _serverOptions = new();

        // ReSharper disable once AsyncVoidMethod
        private async void OnEnable()
        {
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
                _log.Info($"add server ({hostingOption.Text}): {hostingOption.Url}");
                _serverOptions.Add(new(hostingOption.Text, hostingOption.Url));
            }

            serverDropdown.options.Clear();
            serverDropdown.options.AddRange(_serverOptions.Select(x => new TMP_Dropdown.OptionData(x.Text)));
            serverDropdown.RefreshShownValue();
            serverDropdown.onValueChanged.RemoveAllListeners();
            serverDropdown.onValueChanged.AddListener(OnServerChanged);

            serverRequestButton.onClick.RemoveAllListeners();
            serverRequestButton.onClick.AddListener(OnServerRequestButtonClick);
        }

        private IWebClient _webClient;

        public IWebClient ServerWebClient
        {
            get
            {
                if (_webClient == null)
                {
                    var url = _serverOptions[serverDropdown.value].Url;
                    _webClient = new UnityWebClient(url);
                }

                return _webClient;
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

        private static async ValueTask<ServerOption> NeedServerHostingOption()
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

        private void OnServerChanged(int arg0)
        {
            if (_webClient != null)
            {
                _webClient.Dispose();
                _webClient = null;
            }
        }

        private void OnServerRequestButtonClick() =>
            _ = infoControl.ExecuteTextThrobber(
                async text =>
                {
                    text("Requesting...");
                    var webClient = ServerWebClient;
                    using var meta = new MetaClient(webClient, Slog.Factory);
                    var result = await meta.GetInfo(destroyCancellationToken);
                    text($"Response: {WebSerializer.SerializeObject(result, true)}");
                },
                (text, ex) =>
                {
                    text($"ERROR:\n{ex.Message}");
                });
    }
}