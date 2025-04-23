using System;

namespace Shared.Tp.St
{
    public interface IReader<out T>
    {
        T Deserialize(ReadOnlySpan<byte> span);
    }
}