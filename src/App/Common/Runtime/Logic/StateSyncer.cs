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
        void ReceivedRemoteState(TRemote remoteState);
    }

    public class StateSyncer<TLocal, TRemote> : IDisposable
    {
        private readonly ISyncHandler<TLocal, TRemote> _handler;
        private readonly ICmdSender<TLocal> _cmdLink;

        private int _updateSendFrame;
        private float _updateElapsedTime;

        private TRemote? _remoteState;

        public TRemote RemoteState =>
            _remoteState ?? throw new InvalidOperationException("Remote state is not received yet");

        public StateSyncer(ISyncHandler<TLocal, TRemote> handler, ICmdSender<TLocal> cmdLink)
        {
            _handler = handler;
            _cmdLink = cmdLink;
        }

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
            if (_updateElapsedTime > sendInterval)
            {
                _updateElapsedTime = 0;
                return true;
            }

            return false;
        }

        public void RemoteUpdate(TRemote remoteState)
        {
            _remoteState = remoteState;
            _handler.ReceivedRemoteState(_remoteState);
        }
    }
}