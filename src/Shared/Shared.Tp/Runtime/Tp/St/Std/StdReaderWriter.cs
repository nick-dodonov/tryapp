using System;
using System.Buffers;
using System.Diagnostics;
using Shared.Web;

namespace Shared.Tp.St.Std
{
    public class StdOwnWriter<TState> : IOwnWriter
    {
        private readonly TState _state;

        public StdOwnWriter(TState state)
        {
            Debug.Assert(state != null);
            _state = state;
        }

        public override string ToString() => _state!.ToString();

        int IOwnWriter.Serialize(IBufferWriter<byte> writer) 
            => WebSerializer.Default.SerializeTo(writer, _state);
    }

    public class StdReader<TState> : IReader<TState>
    {
        TState IReader<TState>.Deserialize(ReadOnlySpan<byte> span)
            => WebSerializer.Default.Deserialize<TState>(span);
    }
}