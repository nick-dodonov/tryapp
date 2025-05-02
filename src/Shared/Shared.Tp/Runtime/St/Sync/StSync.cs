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

        private readonly History<TLocal> _localHistory = new();
        private readonly History<TRemote> _remoteHistory = new();

        public TRemote RemoteState => _remoteHistory.LastValue;

        public int LocalHistoryCount => _localHistory.Count;
        public int RemoteHistoryCount => _remoteHistory.Count;
        public History<TRemote> RemoteHistory => _remoteHistory;

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
                From = _localHistory.FirstFrame,
                To = ++_localFrame,
                Known = _remoteHistory.LastFrame,

                Value = _handler.MakeLocalState() //TODO: From->To
            };
            _localHistory.AddValue(cmd.To, cmd.Value);
            
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
            _localHistory.ClearUntil(cmd.Known);

            _remoteHistory.ClearUntil(cmd.From);
            _remoteHistory.AddValue(cmd.To, cmd.Value); //TODO: From->To

            _handler.RemoteUpdated();
        }

        void ICmdReceiver<StCmd<TRemote>>.CmdDisconnected()
            => _handler.RemoteDisconnected();
    }
}