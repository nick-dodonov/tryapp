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

    public delegate string LinkIdProvider<in TState>(TState state);

    public class StdLocalStateProvider<TState> : IHandLocalStateProvider<TState>
    {
        private readonly TState _state;
        private readonly LinkIdProvider<TState> _linkIdProvider;

        public StdLocalStateProvider(TState state, LinkIdProvider<TState> linkIdProvider)
        {
            _state = state;
            _linkIdProvider = linkIdProvider;
        }

        string IHandBaseStateProvider<TState>.GetLinkId(TState state) => _linkIdProvider(state);
        TState IHandLocalStateProvider<TState>.ProvideState() => _state;
        int IHandLocalStateProvider<TState>.Serialize(IBufferWriter<byte> writer, TState state) 
            => WebSerializer.Default.SerializeTo(writer, state);
    }

    public class StdRemoteStateProvider<TState> : IHandRemoteStateProvider<TState>
    {
        private readonly LinkIdProvider<TState> _linkIdProvider;

        public StdRemoteStateProvider(LinkIdProvider<TState> linkIdProvider) 
            => _linkIdProvider = linkIdProvider;

        string IHandBaseStateProvider<TState>.GetLinkId(TState state) => _linkIdProvider(state);
        TState IHandRemoteStateProvider<TState>.Deserialize(ReadOnlySpan<byte> span)
            => WebSerializer.Default.Deserialize<TState>(span);
    }
}