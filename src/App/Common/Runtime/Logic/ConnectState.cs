using System;
using System.Buffers;
using Shared.Boot.Version;
using Shared.Tp.Ext.Hand;
using Shared.Web;

namespace Common.Logic
{
    [Serializable]
    public class ConnectState
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
    
    public class ConnectStateProvider : IHandStateProvider<ConnectState>
    {
        private readonly ConnectState _state;
        public ConnectStateProvider(ConnectState state) => _state = state;

        ConnectState IHandStateProvider<ConnectState>.ProvideState() => _state;
        string IHandStateProvider<ConnectState>.GetLinkId(ConnectState state) => state.LinkId;

        int IHandStateProvider<ConnectState>.Serialize(IBufferWriter<byte> writer, ConnectState state) 
            => WebSerializer.Default.SerializeTo(writer, state);

        ConnectState IHandStateProvider<ConnectState>.Deserialize(ReadOnlySpan<byte> span)
            => WebSerializer.Default.Deserialize<ConnectState>(span);
    }
}