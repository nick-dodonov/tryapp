using System;
using UnityEngine;
using UnityEngine.Scripting;

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

        void RemoteUpdated(TRemote remoteState);
        void RemoteDisconnected();
    }

    public class StateSyncer<TLocal, TRemote> : IDisposable, ICmdReceiver<TRemote>
    {
        private readonly ISyncHandler<TLocal, TRemote> _handler;
        private ICmdSender<TLocal> _cmdLink = null!;

        private int _updateSendFrame;
        private float _updateElapsedTime;

        private TRemote? _remoteState;

        public TRemote RemoteState =>
            _remoteState ?? throw new InvalidOperationException("Remote state is not received yet");

        public StateSyncer(ISyncHandler<TLocal, TRemote> handler) 
            => _handler = handler;
        public void Init(ICmdSender<TLocal> cmdLink) 
            => _cmdLink = cmdLink;

        public void Dispose() { }

        public void LocalUpdate(float deltaTime)
        {
            if (!CanSend(deltaTime))
                return;

            var localState = _handler.MakeLocalState(_updateSendFrame++);
            _cmdLink.CmdSend(in localState);
        }

        private bool CanSend(float deltaTime)
        {
            var options = _handler.Options;
            var basicSendRate = options.BasicSendRate;
            if (basicSendRate <= 0)
                return true;

            var sendInterval = 1.0f / basicSendRate;
            _updateElapsedTime += deltaTime;
            if (_updateElapsedTime < sendInterval)
                return false;

            _updateElapsedTime = 0;
            return true;
        }

        void ICmdReceiver<TRemote>.CmdReceived(in TRemote cmd)
        {
            _remoteState = cmd;
            _handler.RemoteUpdated(_remoteState);
        }

        void ICmdReceiver<TRemote>.CmdDisconnected() 
            => _handler.RemoteDisconnected();
    }
}