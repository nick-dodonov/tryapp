using System;
using System.Buffers;
using Shared.Boot.Version;
using Shared.Tp.Ext.Hand;
using Shared.Web;

namespace Common.Logic
{
    [Serializable]
    public class ClientConnectState
    {
        public string PeerId = string.Empty;

        public ClientConnectState() {} // deserialization
        public ClientConnectState(string peerId) => PeerId = peerId;

        public override string ToString() => $"ClientConnectState({PeerId})";
    }

    [Serializable]
    public class ServerConnectState
    {
        public BuildVersion BuildVersion;

        public ServerConnectState() {} // deserialization
        public ServerConnectState(BuildVersion buildVersion) => BuildVersion = buildVersion;

        public override string ToString() => $"ServerConnectState(\"{BuildVersion.ToShortInfo()}\")";
    }
}