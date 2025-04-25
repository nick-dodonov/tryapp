using System;

namespace Shared.Tp.Obj
{
    public interface IObjReader<out T>
    {
        T Deserialize(ReadOnlySpan<byte> span);
    }
}