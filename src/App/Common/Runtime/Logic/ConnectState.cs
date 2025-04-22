using System;
using System.Buffers;
using Shared.Boot.Version;
using Shared.Tp.Ext.Hand;
using Shared.Web;

namespace Common.Logic
{
    public class ConnectStateProvider : IHandStateProvider
    {
        private readonly ConnectState _state;
        public ConnectStateProvider(ConnectState state) => _state = state;

        IHandConnectState IHandStateProvider.ProvideState() => _state;
        public string GetLinkId(IHandConnectState state) => ((ConnectState)state).LinkId;

        int IHandStateProvider.Serialize(IBufferWriter<byte> writer, IHandConnectState state) 
            => WebSerializer.Default.SerializeTo(writer, (ConnectState)state);

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