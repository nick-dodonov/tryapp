using System;
using System.Threading;
using Client.Utility;
using Locator.Client;
using Shared.Log;
using Shared.Web;
using UnityEditor;

namespace Client.Editor
{
    public static class MenuCommands
    {
        
        [MenuItem("[App]/Debug Locator")]
        public static async void DebugLocator()
        {
            try
            {
                Slog.Info("request");
                var options = await OptionsReader.TryReadOptions();
                var webClient = new UnityWebClient(options.Locator);
                var locator = new ClientLocator(webClient);
                var stands = await locator.GetStands(CancellationToken.None);
                Slog.Info($"result: {stands.Length} stands:\n{WebSerializer.Default.Serialize(stands, true)}");
            }
            catch (Exception e)
            {
                Slog.Error($"{e}");
            }
        }
    }
}