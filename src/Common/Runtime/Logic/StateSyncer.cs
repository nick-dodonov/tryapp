using System;
using Shared.Tp;
using Shared.Web;
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

    public interface ISyncHandler<out TLocal>
    {
        SyncOptions Options { get; }
        TLocal MakeLocalState(int sendIndex);
    }

    public class StateSyncer<TLocal, TRemote> : IDisposable
    {
        private readonly ISyncHandler<TLocal> _handler;
        private readonly ITpLink _link;

        private int _updateSendFrame;
        private float _updateElapsedTime;

        private TRemote? _remoteState;
        public TRemote RemoteState =>
            _remoteState ?? throw new InvalidOperationException("remote state is not received yet");

        public StateSyncer(ISyncHandler<TLocal> handler, ITpLink link)
        {
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

        public TRemote RemoteUpdate(ReadOnlySpan<byte> span) => 
            _remoteState = WebSerializer.Default.Deserialize<TRemote>(span);
    }
}