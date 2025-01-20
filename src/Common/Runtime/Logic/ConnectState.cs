using System;
using System.Text;
using Shared.Web;

namespace Common.Logic
{
    public class ConnectStateProvider
    {
        private readonly ConnectState? _connectState;
        public ConnectStateProvider(ConnectState? connectState) => _connectState = connectState;
        public ConnectState ProvideConnectState() => _connectState!;

        public byte[] Serialize(ConnectState state)
        {
            var str = WebSerializer.SerializeObject(this);
            return Encoding.UTF8.GetBytes(str);
        }

        public ConnectState Deserialize(Span<byte> bytes)
        {
            var str = Encoding.UTF8.GetString(bytes);
            return WebSerializer.DeserializeObject<ConnectState>(str);
        }        
    }

    [Serializable]
    public class ConnectState
    {
        public string PeerId;
        public ConnectState(string peerId)
        {
            PeerId = peerId;
        }
        public override string ToString() => $"ConnectState({PeerId})"; //diagnostics only
    }
}