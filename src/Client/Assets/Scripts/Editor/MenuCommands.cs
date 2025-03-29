using System;
using System.Threading;
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
                var locator = new ClientLocator();
                var stands = await locator.GetStands(CancellationToken.None);
                Slog.Info($"result: {WebSerializer.Default.Serialize(stands)}");
            }
            catch (Exception e)
            {
                Slog.Error($"{e}");
            }
        }
    }
}