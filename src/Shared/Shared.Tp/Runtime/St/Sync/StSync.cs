using System;
using Shared.Tp.St.Cmd;

namespace Shared.Tp.St.Sync
{
    public class StSync<TLocal, TRemote> : IDisposable, ICmdReceiver<StCmd<TRemote>>
    {
        private readonly ISyncHandler<TLocal, TRemote> _handler;
        private CmdLink<StCmd<TLocal>, StCmd<TRemote>> _cmdLink = null!;

        public ITpReceiver Receiver => _cmdLink;
        public ITpLink Link => _cmdLink.Link;

        private int _localFrame;
        private float _localElapsedToSend;

        private const int HistoryInitCapacity = 4;
        private readonly StHistory<TLocal> _localHistory = new(HistoryInitCapacity);
        private readonly StHistory<TRemote> _remoteHistory = new(HistoryInitCapacity);

        public StHistory<TLocal> LocalHistory => _localHistory;
        public StHistory<TRemote> RemoteHistory => _remoteHistory;

        public int RemoteStateMs => _remoteHistory.LastKeyOrDefault.Ms;
        public ref TRemote RemoteStateRef => ref _remoteHistory.LastValueRef;

        internal StSync(ISyncHandler<TLocal, TRemote> handler)
            => _handler = handler;

        internal void SetCmdLink(CmdLink<StCmd<TLocal>, StCmd<TRemote>> cmdLink)
            => _cmdLink = cmdLink;

        public void Dispose()
            => _cmdLink.Dispose();

        public void LocalUpdate(float deltaTime)
        {
            if (!CanSend(deltaTime))
                return;

            var cmd = new StCmd<TLocal>
            {
                From = _localHistory.FirstKeyOrDefault.Frame,
                To = ++_localFrame,
                Known = _remoteHistory.LastKeyOrDefault.Frame,
                
                Ms = _handler.TimeMs,

                Value = _handler.MakeLocalState() //TODO: From->To
            };
            _localHistory.AddValueRef((cmd.To, cmd.Ms)) = cmd.Value;
            
            _cmdLink.CmdSend(in cmd);
        }

        private bool CanSend(float deltaTime)
        {
            var options = _handler.Options;
            var basicSendRate = options.BasicSendRate;
            if (basicSendRate <= 0)
                return true;

            var sendInterval = 1.0f / basicSendRate;
            _localElapsedToSend += deltaTime;
            if (_localElapsedToSend < sendInterval)
                return false;

            _localElapsedToSend = 0;
            return true;
        }

        void ICmdReceiver<StCmd<TRemote>>.CmdReceived(in StCmd<TRemote> cmd)
        {
            _localHistory.ClearUntil((cmd.Known, 0)); //TODO: think to move to filling local state

            _remoteHistory.ClearUntil((cmd.From - 2, 0)); //TODO: XXXXXXXX ClearUntil interpolation to keep
            _remoteHistory.AddValueRef((cmd.To, cmd.Ms)) = cmd.Value; //TODO: From->To

            _handler.RemoteUpdated();
        }

        void ICmdReceiver<StCmd<TRemote>>.CmdDisconnected()
            => _handler.RemoteDisconnected();
    }
}