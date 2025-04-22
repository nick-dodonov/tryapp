using System;
using System.Buffers;
using Shared.Tp.Ext.Hand;
using Shared.Web;

namespace Common.Logic.Shared
{
    public class StdLocalStateProvider<TState> : IHandLocalStateProvider<TState>
    {
        private readonly TState _state;

        public StdLocalStateProvider(TState state) => _state = state;

        TState IHandLocalStateProvider<TState>.ProvideState() => _state;
        int IHandLocalStateProvider<TState>.Serialize(IBufferWriter<byte> writer, TState state) 
            => WebSerializer.Default.SerializeTo(writer, state);
    }

    public class StdRemoteStateProvider<TState> : IHandRemoteStateProvider<TState>
    {
        TState IHandRemoteStateProvider<TState>.Deserialize(ReadOnlySpan<byte> span)
            => WebSerializer.Default.Deserialize<TState>(span);
    }
}