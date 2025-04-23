using System;
using System.Buffers;
using Shared.Tp.Ext.Hand;
using Shared.Web;

namespace Common.Logic.Shared
{
    public class StdLocalStateProvider<TState> : IHandLocalStateProvider
    {
        private readonly TState _state;

        public StdLocalStateProvider(TState state) => _state = state;
        public override string ToString() => _state!.ToString();

        int IHandLocalStateProvider.Serialize(IBufferWriter<byte> writer) 
            => WebSerializer.Default.SerializeTo(writer, _state);
    }

    public class StdRemoteStateProvider<TState> : IHandRemoteStateProvider<TState>
    {
        TState IHandRemoteStateProvider<TState>.Deserialize(ReadOnlySpan<byte> span)
            => WebSerializer.Default.Deserialize<TState>(span);
    }
}