using System;
using System.Threading.Tasks;
using Shared.Log;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Client.UI
{
    public interface ISessionController
    {
        Task StartSession();
        void StopSession();
    }
    
    /// <summary>
    /// Provides fool-protection against multiple starts
    /// </summary>
    public class SessionControl : MonoBehaviour
    {
        private static readonly Slog.Area _log = new();
        
        public Button actionButton;
        public TMP_Text actionText;

        private enum State
        {
            Stopped,
            Starting,
            Started
        }
        private State _state = State.Stopped;
        public ISessionController Controller { get; set; }

        private void OnEnable()
        {
            actionButton.onClick.AddListener(() =>
            {
                switch (_state)
                {
                    case State.Stopped: StartSession(); break;
                    case State.Started: StopSession(); break;
                    case State.Starting:
                    default: 
                        throw new InvalidOperationException("must not be interactable");
                }
            });
            UpdateState(State.Stopped);
        }

        private async void StartSession() //TODO: add FireAndForget for async Task 
        {
            if (_state != State.Stopped) throw new InvalidOperationException($"invalid state: {_state}");
            UpdateState(State.Starting);
            try
            {
                await Controller.StartSession();

                _log.Info("succeed");
                UpdateState(State.Started);
            }
            catch (Exception e)
            {
                _log.Info($"failed: {e}");
                UpdateState(State.Stopped);
            }
        }

        private void StopSession()
        {
            if (_state != State.Started) throw new InvalidOperationException($"invalid state: {_state}");
            UpdateState(State.Stopped);
            Controller.StopSession();
        }

        private void UpdateState(State state)
        {
            if (_state != state)
                _log.Info($"{_state} -> {state}");
            _state = state;
            switch (_state)
            {
                case State.Stopped:
                    actionText.text = "Start";
                    actionText.color = new Color32(0, 180, 0, 0xFF);
                    actionButton.interactable = true;
                    break;
                case State.Starting:
                    actionText.text = "Starting...";
                    actionText.color = new Color32(180, 180, 0, 0xFF);
                    actionButton.interactable = false;
                    break;
                case State.Started:
                    actionText.text = "Stop";
                    actionText.color = new Color32(180, 0, 0, 0xFF);
                    actionButton.interactable = true;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
