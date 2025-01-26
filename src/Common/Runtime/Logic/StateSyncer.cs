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

    public interface ISyncHandler<out TState>
    {
        SyncOptions Options { get; }
        TState MakeState(int sendIndex);
    }

    public class StateSyncer<TState> : IDisposable
    {
        private readonly ISyncHandler<TState> _handler;
        private readonly ITpLink _link;

        private int _updateSendFrame;
        private float _updateElapsedTime;

        public StateSyncer(ISyncHandler<TState> handler, ITpLink link)
        {
            _handler = handler;
            _link = link;
        }

        public void Dispose() { }

        public void LocalUpdate(float deltaTime)
        {
            if (!CanSend(deltaTime))
                return;

            var state = _handler.MakeState(_updateSendFrame++);

            _link.Send(WebSerializer.Default.Serialize, in state);
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
    }
}