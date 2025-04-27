using System;
using Shared.Tp.Data;
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

    public struct StCmd<T>
    {
        public int Frame;
        public T Value; // to be changed
    }

    public interface ISyncHandler<TLocal, TRemote>
    {
        SyncOptions Options { get; }

        IObjWriter<StCmd<TLocal>> LocalWriter { get; }
        IObjReader<StCmd<TRemote>> RemoteReader { get; }

        TLocal MakeLocalState(int sendIndex);

        void RemoteUpdated(TRemote remoteState);
        void RemoteDisconnected();
    }

    public class StateSyncer<TLocal, TRemote> : IDisposable, ICmdReceiver<StCmd<TRemote>>
    {
        private readonly ISyncHandler<TLocal, TRemote> _handler;

        private CmdLink<StCmd<TLocal>, StCmd<TRemote>> _cmdLink = null!;

        public ITpReceiver Receiver => _cmdLink;
        public ITpLink Link => _cmdLink.Link;

        private int _updateSendFrame;
        private float _updateElapsedTime;

        private TRemote? _remoteState;
        public TRemote RemoteState =>
            _remoteState ?? throw new InvalidOperationException("Remote state is not received yet");

        internal StateSyncer(ISyncHandler<TLocal, TRemote> handler) 
            => _handler = handler;
        internal void SetCmdLink(CmdLink<StCmd<TLocal>, StCmd<TRemote>> cmdLink) 
            => _cmdLink = cmdLink;

        public void Dispose()
        {
            _cmdLink.Dispose();
        }

        public void LocalUpdate(float deltaTime)
        {
            if (!CanSend(deltaTime))
                return;

            var localSt = new StCmd<TLocal>
            {
                Frame = _updateSendFrame, 
                Value = _handler.MakeLocalState(_updateSendFrame++)
            };
            _cmdLink.CmdSend(in localSt);
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

        void ICmdReceiver<StCmd<TRemote>>.CmdReceived(in StCmd<TRemote> cmd)
        {
            _remoteState = cmd.Value;
            _handler.RemoteUpdated(_remoteState);
        }

        void ICmdReceiver<StCmd<TRemote>>.CmdDisconnected() 
            => _handler.RemoteDisconnected();
    }
}