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

        private void SetText(string text) => infoText.text = text;

        public async Task ExecuteTextThrobber(
            Func<Action<string>, Task> action,
            Action<Action<string>, Exception> errorAction,
            [CallerMemberName] string caller = "")
        {
            _log.Info(caller);
            try
            {
                await action(SetText);
            }
            catch (Exception ex)
            {
                errorAction(SetText, ex);
                throw;
            }
        }
    }
}