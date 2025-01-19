using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Client.Logic;
using Client.UI;
using Client.Utility;
using Common.Meta;
using Shared.Audit;
using Shared.Log;
using Shared.Web;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Client
{
    public class HudLogic : MonoBehaviour, ISessionController
    {
        private static readonly Slog.Area _log = new();

        public TMP_Text versionText;

        public TMP_Dropdown serverDropdown;
        public Button serverRequestButton;
        public TMP_Text serverResponseText;

        public SessionControl sessionControl;
        public ClientSession clientSession;

        private record ServerOption(string Text, string Url);

        private readonly List<ServerOption> _serverOptions = new();

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
        }

        private void OnDisable()
        {
            StopSession("closing");
            serverRequestButton.onClick.RemoveListener(OnServerRequestButtonClick);
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

        async Task ISessionController.StartSession(CancellationToken cancellationToken)
        {
            try
            {
                _log.Info(".");
                serverResponseText.text = "Starting...";
                await clientSession.Begin(
                    CreateWebClient,
                    StopSession,
                    cancellationToken);
                serverResponseText.text = "RESULT:\nOK";
            }
            catch (Exception ex)
            {
                serverResponseText.text = $"ERROR:\n{ex.Message}";
                StopSession("connect error");
                throw;
            }
        }

        void ISessionController.StopSession()
        {
            _log.Info(".");
            StopSession("user request");
        }

        private void StopSession(string reason)
        {
            _log.Info(reason);
            try
            {
                clientSession.Finish(reason);
                _log.Info("client session finished");
            }
            catch (Exception ex)
            {
                _log.Error($"{ex}");
            }

            _log.Info("notifying session control");
            sessionControl.NotifyStopped();
        }

        private IWebClient CreateWebClient()
        {
            var url = _serverOptions[serverDropdown.value].Url;
            return new UnityWebClient(url);
        }

        private static IMeta CreateMetaClient(IWebClient webClient)
            => new MetaClient(webClient, Slog.Factory);
    }
}