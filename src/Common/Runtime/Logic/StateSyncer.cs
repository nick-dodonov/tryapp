using System;
using Shared.Log;
using Shared.Tp;
using Shared.Web;
using UnityEngine;
using UnityEngine.Scripting;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Common.Logic
{
    [Serializable]
    public class SyncOptions
    {
        [field: SerializeField] [RequiredMember]
        public int BasicSendRate { get; set; } = 1;
    }

    public interface ISyncHandler<out TLocal, in TRemote>
    {
        SyncOptions Options { get; }
        TLocal MakeLocalState(int sendIndex);
        void ReceivedRemoteState(TRemote remoteState);
    }

    public class StateSyncer<TLocal, TRemote> : IDisposable
    {
        private readonly ILogger _logger;

        private readonly ISyncHandler<TLocal, TRemote> _handler;
        private readonly ITpLink _link;

        private int _updateSendFrame;
        private float _updateElapsedTime;

        private TRemote? _remoteState;

        public TRemote RemoteState =>
            _remoteState ?? throw new InvalidOperationException("Remote state is not received yet");

        public StateSyncer(ISyncHandler<TLocal, TRemote> handler, ITpLink link, ILogger logger)
        {
            _logger = logger;
            _handler = handler;
            _link = link;
        }

        public void Dispose() { }

        public void LocalUpdate(float deltaTime)
        {
            if (!CanSend(deltaTime))
                return;

            var localState = _handler.MakeLocalState(_updateSendFrame++);

            _link.Send(WebSerializer.Default.Serialize, in localState);
        }

        private bool CanSend(float deltaTime)
        {
            var options = _handler.Options;
            var basicSendRate = options.BasicSendRate;
            if (basicSendRate <= 0)
                return true;

            var sendInterval = 1.0f / basicSendRate;
            _updateElapsedTime += deltaTime;
            if (_updateElapsedTime > sendInterval)
            {
                _updateElapsedTime = 0;
                return true;
            }

            return false;
        }

        public void RemoteUpdate(ReadOnlySpan<byte> span)
        {
            try
            {
                _remoteState = WebSerializer.Default.Deserialize<TRemote>(span);
                _handler.ReceivedRemoteState(_remoteState);
            }
            catch (Exception e)
            {
                _logger.Error($"{e}");
            }
        }
    }
}