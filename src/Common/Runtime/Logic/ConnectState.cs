using System;
using System.Text;
using Shared.Tp.Hand;
using Shared.Web;

namespace Common.Logic
{
    public class ConnectStateProvider : IHandStateProvider
    {
        private readonly ConnectState? _connectState;
        public ConnectStateProvider(ConnectState? connectState) => _connectState = connectState;

        IHandConnectState IHandStateProvider.ProvideConnectState() => _connectState!;

        byte[] IHandStateProvider.Serialize(IHandConnectState connectState)
        {
            var str = WebSerializer.SerializeObject(connectState);
            return Encoding.UTF8.GetBytes(str);
        }

        IHandConnectState IHandStateProvider.Deserialize(ReadOnlySpan<byte> span)
        {
            var str = Encoding.UTF8.GetString(span);
            return WebSerializer.DeserializeObject<ConnectState>(str);
        }        
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