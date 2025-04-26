using System;

namespace Shared.Tp.Data
{
    public interface IObjReader<out T>
    {
        T Deserialize(ReadOnlySpan<byte> span);
    }
}