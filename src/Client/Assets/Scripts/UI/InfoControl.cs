using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Shared.Log;
using TMPro;
using UnityEngine;

namespace Client.UI
{
    public class InfoControl : MonoBehaviour
    {
        private static readonly Slog.Area _log = new();

        public TMP_Text infoText;

        private void Awake()
        {
            infoText.text = string.Empty;
        }

        public void SetText(string text) => infoText.text = text;

        public async Task ExecuteTextThrobber(
            Func<Action<string>, Task> action,
            Action<Action<string>, Exception> failed,
            [CallerFilePath] string path = "",
            [CallerMemberName] string caller = "")
        {
            try
            {
                _log.Info($"action: {new Category(path).NameSpan.ToString()}.{caller}");
                await action(SetText);
            }
            catch (Exception ex)
            {
                _log.Warn($"failed: {new Category(path).NameSpan.ToString()}.{caller}: {ex.Message}");
                failed(SetText, ex);
                throw;
            }
        }
    }
}