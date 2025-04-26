using System;
using Shared.Tp.Obj;
using Shared.Tp.St.Cmd;
using UnityEngine;
using UnityEngine.Scripting;

namespace Shared.Tp.St.Sync
{
    [Serializable]
    public class SyncOptions
    {
        [field: SerializeField] [RequiredMember]
        public int BasicSendRate { get; set; } = 1;
    }

    public interface ISyncHandler<TLocal, TRemote>
    {
        SyncOptions Options { get; }

        IObjWriter<TLocal> LocalWriter { get; }
        IObjReader<TRemote> RemoteReader { get; }

        TLocal MakeLocalState(int sendIndex);

        void RemoteUpdated(TRemote remoteState);
        void RemoteDisconnected();
    }

    public class StateSyncer<TLocal, TRemote> : IDisposable, ICmdReceiver<TRemote>
    {
        private readonly ISyncHandler<TLocal, TRemote> _handler;

        private CmdLink<TLocal, TRemote> _cmdLink = null!;
        
        public ITpReceiver Receiver => _cmdLink;
        public ITpLink Link => _cmdLink.Link;

        private int _updateSendFrame;
        private float _updateElapsedTime;

        private TRemote? _remoteState;
        public TRemote RemoteState =>
            _remoteState ?? throw new InvalidOperationException("Remote state is not received yet");

        internal StateSyncer(ISyncHandler<TLocal, TRemote> handler) 
            => _handler = handler;
        internal void SetCmdLink(CmdLink<TLocal, TRemote> cmdLink) 
            => _cmdLink = cmdLink;

        public void Dispose()
        {
            _cmdLink.Dispose();
        }

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