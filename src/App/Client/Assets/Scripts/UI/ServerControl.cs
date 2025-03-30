using System;
using System.Linq;
using System.Threading;
using Assets.SimpleSpinner;
using Client.Utility;
using Common.Meta;
using Cysharp.Threading.Tasks;
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
        public SimpleSpinner serverSpinner;
        public Button serverRequestButton;

        private ServerList _serverList = new();

        private IWebClient _webClient;
        public IWebClient ServerWebClient
        {
            get
            {
                if (_webClient == null)
                {
                    var url = _serverList[serverDropdown.value].Url;
                    _webClient = new UnityWebClient(url);
                }

                return _webClient;
            }
        }

        private async void OnEnable()
        {
            await ClientOptions.InstanceAsync;

            SetSpinnerActive(false);
            SetServerList(ServerList.CreateDefault());

            serverDropdown.onValueChanged.RemoveAllListeners();
            serverDropdown.onValueChanged.AddListener(OnServerChanged);

            serverRequestButton.onClick.RemoveAllListeners();
            serverRequestButton.onClick.AddListener(OnServerRequestButtonClick);

            UpdateStands(gameObject.GetCancellationTokenOnDestroy());
        }

        private void SetSpinnerActive(bool active) 
            => serverSpinner.gameObject.SetActive(active);

        private void SetServerList(ServerList serverList)
        {
            _serverList = serverList;
            serverDropdown.options.Clear();
            serverDropdown.options.AddRange(_serverList.Select(x => new TMP_Dropdown.OptionData(x.Text)));
            serverDropdown.RefreshShownValue();
            serverDropdown.RefreshOptions();
        }

        private async void UpdateStands(CancellationToken cancellationToken)
        {
            try
            {
                SetSpinnerActive(true);
                var standsList = await ServerList.CreateStandsAsync(cancellationToken);
                SetServerList(standsList);
            }
            catch (Exception e)
            {
                _log.Error($"{e}");
            }
            finally
            {
                SetSpinnerActive(false);
            }
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
                    text($"Response: {WebSerializer.Default.Serialize(result, true)}");
                },
                (text, ex) => { text($"ERROR:\n{ex.Message}"); });
    }
}