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
        {
            var result = MemoryPackSerializer.Deserialize<T>(span);
            if (result != null)
                return result;
            MemoryPackSerializationException.ThrowDeserializeObjectIsNull(typeof(T).Name);
            return default;
        }

        void IObjReader<T>.Deserialize(ReadOnlySpan<byte> span, ref T value) 
            => MemoryPackSerializer.Deserialize(span, ref value!);
    }
}