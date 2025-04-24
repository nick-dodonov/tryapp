using System;
using System.Threading;
using System.Threading.Tasks;
using Shared.Tp;
using UnityEngine;
using UnityEngine.Scripting;

namespace Common.Logic.Shared
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

        private StdCmdLink<TLocal, TRemote> _cmdLink = null!;
        public ITpReceiver Receiver => _cmdLink;
        public ITpLink Link => _cmdLink.Link;

        private int _updateSendFrame;
        private float _updateElapsedTime;

        private TRemote? _remoteState;
        public TRemote RemoteState =>
            _remoteState ?? throw new InvalidOperationException("Remote state is not received yet");

        private StateSyncer(ISyncHandler<TLocal, TRemote> handler) 
            => _handler = handler;

        public static StateSyncer<TLocal, TRemote> CreateConnected(
            ISyncHandler<TLocal, TRemote> handler, 
            ITpLink link)
        {
            var syncer = new StateSyncer<TLocal, TRemote>(handler);
            syncer._cmdLink = new(syncer, link);
            return syncer;
        }
        
        public static async ValueTask<StateSyncer<TLocal, TRemote>> CreateAndConnect(
            ISyncHandler<TLocal, TRemote> handler,
            ITpApi api, CancellationToken cancellationToken)
        {
            var syncer = new StateSyncer<TLocal, TRemote>(handler);
            syncer._cmdLink = await StdCmdLink<TLocal, TRemote>.Connect(syncer, api,cancellationToken);
            return syncer;
        }

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