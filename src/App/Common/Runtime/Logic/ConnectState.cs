using System;
using System.Buffers;
using Shared.Tp.Ext.Hand;
using Shared.Web;

namespace Common.Logic
{
    public class ConnectStateProvider : IHandStateProvider
    {
        private readonly ConnectState? _connectState;
        public ConnectStateProvider(ConnectState? connectState) => _connectState = connectState;

        IHandConnectState IHandStateProvider.ProvideConnectState() => _connectState!;

        void IHandStateProvider.Serialize(IBufferWriter<byte> writer, IHandConnectState connectState) 
            => WebSerializer.Default.Serialize(writer, connectState);

        IHandConnectState IHandStateProvider.Deserialize(ReadOnlySpan<byte> span)
            => WebSerializer.Default.Deserialize<ConnectState>(span);
    }

    [Serializable]
    public class ConnectState : IHandConnectState
    {
        public string LinkId { get; set; } = string.Empty;

        public ConnectState() {}
        public ConnectState(string linkId) => LinkId = linkId;

        public override string ToString() => $"ConnectState({LinkId})"; //diagnostics only
    }
}