using System;
using System.Buffers;
using Shared.Tp.Ext.Hand;
using Shared.Web;

namespace Common.Logic.Shared
{
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