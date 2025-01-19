using System;
using System.Threading;
using System.Threading.Tasks;
using Shared.Log;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Client.UI
{
    public interface ISessionController
    {
        Task StartSession(CancellationToken cancellationToken);
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
            Started,
            CancellingStart
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
                    case State.Starting: CancelStartingSession(); break;
                    case State.Started: StopSession(); break;
                    case State.CancellingStart:
                    default:
                        throw new InvalidOperationException("must not be interactable");
                }
            });
            ChangeState(State.Stopped);
        }

        public void NotifyStopped()
        {
            ChangeState(State.Stopped);
        }

        private CancellationTokenSource _startingTcs;

        private async void StartSession() //TODO: add FireAndForget for async Task 
        {
            try
            {
                if (_state != State.Stopped) throw new InvalidOperationException($"invalid state: {_state}");
                ChangeState(State.Starting);

                _startingTcs = new(); //TODO: Linked CTS with destroyCancellationToken
                await Controller.StartSession(_startingTcs.Token);
                _startingTcs.Token.ThrowIfCancellationRequested();

                _log.Info("succeed");
                ChangeState(State.Started);
            }
            catch (Exception e)
            {
                _log.Warn($"failed: {e}");
                ChangeState(State.Stopped);
            }
            finally
            {
                _startingTcs?.Dispose();
            }
        }

        private void CancelStartingSession()
        {
            if (_state != State.Starting) throw new InvalidOperationException($"invalid state: {_state}");
            ChangeState(State.CancellingStart);
        }

        private void StopSession()
        {
            if (_state != State.Started) throw new InvalidOperationException($"invalid state: {_state}");
            ChangeState(State.Stopped);
            Controller.StopSession();
        }

        private void ChangeState(State state)
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
                    actionText.text = "Cancel\nStarting...";
                    actionText.color = new Color32(180, 180, 0, 0xFF);
                    actionButton.interactable = true;
                    break;
                case State.Started:
                    actionText.text = "Stop";
                    actionText.color = new Color32(180, 0, 0, 0xFF);
                    actionButton.interactable = true;
                    break;
                case State.CancellingStart:
                    actionText.text = "Cancelling\nStart...";
                    actionText.color = new Color32(180, 180, 0, 0xFF);
                    actionButton.interactable = false;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}