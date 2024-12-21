using System;
using System.Net.Http;
using Shared.Meta.Client;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HudLogic : MonoBehaviour
{
    public TMP_Text versionText;
    public TMP_Dropdown serverDropdown;
    public Button serverRequestButton;
    public TMP_Text serverResponseText;

    private void OnEnable()
    {
        Shared.StaticLog.Info("==== starting client ====");
        versionText.text = $"Version: {Application.version}";
        
        serverDropdown.options.Clear();
#if UNITY_EDITOR
        serverDropdown.options.Add(new TMP_Dropdown.OptionData("http://localhost:5270"));
#endif
        serverDropdown.options.Add(new TMP_Dropdown.OptionData("https://TODO-BuildOption"));
        serverRequestButton.onClick.AddListener(OnServerRequestButtonClick);
        serverResponseText.text = "";
    }

    private void OnDisable()
    {
        serverRequestButton.onClick.RemoveListener(OnServerRequestButtonClick);
    }

    private async void OnServerRequestButtonClick()
    {
        try
        {
            serverResponseText.text = "Requesting server...";
            var selectedServer = serverDropdown.options[serverDropdown.value].text;
            var serverAddress = new Uri(selectedServer);
            var client = new HttpClient { BaseAddress = serverAddress };
            var meta = new MetaClient(client);
            var result = await meta.GetInfo(destroyCancellationToken);
            serverResponseText.text = $"Server Response: {result.RequestTime}";
        }
        catch (Exception ex)
        {
            serverResponseText.text = $"Error: {ex.Message}";
        }
    }
}
