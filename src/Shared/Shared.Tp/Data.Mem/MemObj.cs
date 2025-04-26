using System;
using System.Buffers;
using MemoryPack;

namespace Shared.Tp.Data.Mem
{
    public class MemObjWriter<T> : IObjWriter<T>
    {
        void IObjWriter<T>.Serialize(IBufferWriter<byte> writer, T obj) 
            => MemoryPackSerializer.Serialize(in writer, in obj);
    }

    public class MemObjReader<T> : IObjReader<T>
    {
        T IObjReader<T>.Deserialize(ReadOnlySpan<byte> span) 
            => MemoryPackSerializer.Deserialize<T>(span) ?? throw new InvalidOperationException("Failed to deserialize");
    }
}