using System;
using System.Buffers;
using Shared.Web;

namespace Shared.Tp.St.Std
{
    public class StdOwnStateWriter<TState> : IOwnStateWriter
    {
        private readonly TState _state;

        public StdOwnStateWriter(TState state) => _state = state;
        public override string ToString() => _state!.ToString();

        int IOwnStateWriter.Serialize(IBufferWriter<byte> writer) 
            => WebSerializer.Default.SerializeTo(writer, _state);
    }

    public class StdRemoteStateReader<TState> : IStateReader<TState>
    {
        TState IStateReader<TState>.Deserialize(ReadOnlySpan<byte> span)
            => WebSerializer.Default.Deserialize<TState>(span);
    }
}