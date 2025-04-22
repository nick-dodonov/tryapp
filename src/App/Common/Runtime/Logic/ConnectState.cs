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
        public string LinkId = string.Empty;
        public BuildVersion BuildVersion;

        public ConnectState() {} // for deserialization
        public ConnectState(string linkId, BuildVersion buildVersion)
        {
            LinkId = linkId;
            BuildVersion = buildVersion;
        }

        public override string ToString() => $"ConnectState({LinkId} \"{BuildVersion.ToShortInfo()}\")"; //diagnostics only
    }
    
    public class StdLocalStateProvider<TState> : IHandLocalStateProvider<TState>
    {
        private readonly TState _state;
        private readonly LinkIdProvider _linkIdProvider;

        public delegate string LinkIdProvider(TState state);
        public StdLocalStateProvider(TState state, LinkIdProvider linkIdProvider)
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
        private readonly LinkIdProvider _linkIdProvider;

        public delegate string LinkIdProvider(TState state);
        public StdRemoteStateProvider(LinkIdProvider linkIdProvider) 
            => _linkIdProvider = linkIdProvider;

        string IHandBaseStateProvider<TState>.GetLinkId(TState state) => _linkIdProvider(state);
        TState IHandRemoteStateProvider<TState>.Deserialize(ReadOnlySpan<byte> span)
            => WebSerializer.Default.Deserialize<TState>(span);
    }
}