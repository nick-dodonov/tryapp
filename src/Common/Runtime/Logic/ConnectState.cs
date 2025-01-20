using System;
using System.Text;
using Shared.Web;

namespace Common.Logic
{
    [Serializable]
    public class ConnectState
    {
        public string PeerId;
        public ConnectState(string peerId)
        {
            PeerId = peerId;
        }
        public override string ToString() => $"ConnectState({PeerId})"; //diagnostics only

        public byte[] Serialize()
        {
            var str = WebSerializer.SerializeObject(this);
            return Encoding.UTF8.GetBytes(str);
        }

        public static ConnectState Deserialize(Span<byte> bytes)
        {
            var str = Encoding.UTF8.GetString(bytes);
            return WebSerializer.DeserializeObject<ConnectState>(str);
        }        
    }
}