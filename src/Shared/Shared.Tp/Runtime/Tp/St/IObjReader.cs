using System;

namespace Shared.Tp.St
{
    public interface IObjReader<out T>
    {
        T Deserialize(ReadOnlySpan<byte> span);
    }
}