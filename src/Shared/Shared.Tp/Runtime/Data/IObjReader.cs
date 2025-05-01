using System;

namespace Shared.Tp.Data
{
    public interface IObjReader<T>
    {
        T Deserialize(ReadOnlySpan<byte> span);
        void Deserialize(ReadOnlySpan<byte> span, ref T value);
    }
}