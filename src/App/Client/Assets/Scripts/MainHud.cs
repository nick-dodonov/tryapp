using System;
using System.Threading;
using System.Threading.Tasks;
using Client.Logic;
using Client.UI;
using Client.Utility;
using Shared.Boot.Audit;
using Shared.Boot.Version;
using Shared.Log;
using UnityEngine;
using Utilities.Async;

namespace Client
{
    public class MainHud : MonoBehaviour, ISessionController, ISessionWorkflowOperator
    {
        private static readonly Slog.Area _log = new();

        public InfoControl infoControl;
        public ServerControl serverControl;
        public SessionControl sessionControl;
        public ClientSession clientSession;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        // ReSharper disable once AsyncVoidMethod
        private static async void Initialize()
        {
            _log.Info($">>>> starting {Application.productName}: {UnityVersionProvider.BuildVersion.ToShortInfo(true)}");
            await ClientOptions.InstanceAsync;
            // var logger = Slog.Factory.CreateLogger<MainHud>();
            // logger.Info("==== starting client (logger) ====");
            StartupInfo.Print();
            
            //workaround because project setting -> player -> background is not working in editor
            if (Application.isEditor)
                Application.runInBackground = true;

            // workaround for com.utilities.async init: creates CoroutineRunner to handle System.Threading.Timer
            await Awaiters.UnityMainThread; 
            _log.Info("==== running app");
        }

        private void OnEnable()
        {
            sessionControl.Controller = this;
        }

        private void OnDisable()
        {
            StopSession("closing");
        }

        private void OnApplicationPause(bool pauseStatus) => 
            _log.Info($"{pauseStatus}");

        Task ISessionController.StartSession(CancellationToken cancellationToken) =>
            infoControl.ExecuteTextThrobber(async text =>
            {
                text("Starting...");
                await clientSession.Begin(
                    serverControl.ServerWebClient,
                    this,
                    cancellationToken);
                text("RESULT:\nOK");
            }, (text, ex) =>
            {
                text($"ERROR:\n{ex.Message}");
                StopSession("connect error");
            });

        void ISessionController.StopSession() => StopSession("user request");
        void ISessionWorkflowOperator.Disconnected() => StopSession("disconnected");

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