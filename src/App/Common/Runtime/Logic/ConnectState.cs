using System;
using System.Buffers;
using Shared.Boot.Version;
using Shared.Tp.Ext.Hand;
using Shared.Web;

namespace Common.Logic
{
    public class ConnectStateProvider : IHandStateProvider
    {
        private readonly ConnectState _connectState;
        public ConnectStateProvider(ConnectState connectState) => _connectState = connectState;

        IHandConnectState IHandStateProvider.ProvideConnectState() => _connectState;

        int IHandStateProvider.Serialize(IBufferWriter<byte> writer, IHandConnectState connectState) 
            => WebSerializer.Default.SerializeTo(writer, (ConnectState)connectState);

        IHandConnectState IHandStateProvider.Deserialize(ReadOnlySpan<byte> span)
            => WebSerializer.Default.Deserialize<ConnectState>(span);
    }

    [Serializable]
    public class ConnectState : IHandConnectState
    {
        public string LinkId { get; set; } = string.Empty;
        public BuildVersion BuildVersion;

        public ConnectState() {} // for deserialization
        public ConnectState(string linkId, BuildVersion buildVersion)
        {
            LinkId = linkId;
            BuildVersion = buildVersion;
        }

        public override string ToString() => $"ConnectState({LinkId} \"{BuildVersion.ToShortInfo()}\")"; //diagnostics only
    }
}