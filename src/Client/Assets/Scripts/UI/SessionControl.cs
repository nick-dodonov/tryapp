using Shared.Log;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Client.UI
{
    public interface ISessionController
    {
        void StartSession();
        void StopSession();
    }
    
    /// <summary>
    /// Provides fool-protection against multiple starts
    /// TODO: notify "starting" state and prohibit stop in it
    /// </summary>
    public class SessionControl : MonoBehaviour
    {
        private static readonly Slog.Area _log = new();
        
        public Button actionButton;
        public TMP_Text actionText;
        
        private bool _started;
        public ISessionController Controller { get; set; }

        private void OnEnable()
        {
            actionButton.onClick.AddListener(() =>
            {
                SetStarted(!_started);
            });
            BumpView();
        }

        private void SetStarted(bool started)
        {
            if (_started == started)
                return;
            _log.Info($"{_started} -> {started}");

            _started = started;
            BumpView();

            if (Controller == null)
                return;
            if (started)
                Controller.StartSession();
            else
                Controller.StopSession();
        }

        private void BumpView()
        {
            actionText.text = _started ? "Stop" : "Start";
            Color32 color = _started 
                ? new(180, 0, 0, 0xFF) 
                : new(0, 180, 0, 0xFF);
            actionText.color = color;
        }
    }
}
