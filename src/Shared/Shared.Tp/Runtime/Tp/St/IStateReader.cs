using System;

namespace Shared.Tp.St
{
    public interface IStateReader<out TState>
    {
        TState Deserialize(ReadOnlySpan<byte> span);
    }
}