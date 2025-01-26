using System;
using Shared.Tp;
using Shared.Web;

namespace Common.Logic
{
    [Serializable]
    public class SyncOptions
    {
        public int basicSendRate = 1;
    }

    public interface ISyncHandler<out TState>
    {
        TState MakeState(int sendIndex);
    }

    public class StateSyncer<TState> : IDisposable
    {
        private readonly SyncOptions _options;
        private readonly ISyncHandler<TState> _handler;
        private readonly ITpLink _link;

        private int _updateSendFrame;
        private float _updateElapsedTime;

        public StateSyncer(SyncOptions options, ISyncHandler<TState> handler, ITpLink link)
        {
            _options = options;
            _handler = handler;
            _link = link;
        }

        public void Dispose() { }

        public void LocalUpdate(float deltaTime)
        {
            var sendInterval = 1.0f / _options.basicSendRate;
            _updateElapsedTime += deltaTime;
            if (_updateElapsedTime > sendInterval)
            {
                _updateElapsedTime = 0;
                var state = _handler.MakeState(_updateSendFrame++);

                _link.Send(WebSerializer.Default.Serialize, in state);
            }
        }
    }
}