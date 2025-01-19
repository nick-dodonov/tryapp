using System;
using System.Threading;
using System.Threading.Tasks;
using Client.Logic;
using Client.UI;
using Shared.Audit;
using Shared.Log;
using TMPro;
using UnityEngine;

namespace Client
{
    public class HudLogic : MonoBehaviour, ISessionController
    {
        private static readonly Slog.Area _log = new();

        public TMP_Text versionText;

        public InfoControl infoControl;
        public ServerControl serverControl;
        public SessionControl sessionControl;
        public ClientSession clientSession;

        private void OnEnable()
        {
            //await UniTask.Delay(1000).WithCancellation(destroyCancellationToken);
            _log.Info("==== starting client (static) ====");
            // var logger = Slog.Factory.CreateLogger<HudLogic>();
            // logger.Info("==== starting client (logger) ====");

            StartupInfo.Print();
            versionText.text = $"Version: {Application.version}";

            sessionControl.Controller = this;
        }

        private void OnDisable()
        {
            StopSession("closing");
        }

        Task ISessionController.StartSession(CancellationToken cancellationToken) =>
            infoControl.ExecuteTextThrobber(async text =>
            {
                text("Starting...");
                await clientSession.Begin(
                    serverControl.ServerWebClient,
                    StopSession,
                    cancellationToken);
                text("RESULT:\nOK");
            }, (text, ex) =>
            {
                text($"ERROR:\n{ex.Message}");
                StopSession("connect error");
            });

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
    }
}