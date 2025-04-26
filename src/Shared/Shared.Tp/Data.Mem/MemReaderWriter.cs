using System;
using System.Buffers;

namespace Shared.Tp.Data.Mem
{
    public class MemObjReader<T> : IObjReader<T>
    {
        T IObjReader<T>.Deserialize(ReadOnlySpan<byte> span)
            => throw new NotImplementedException();
    }

    public class MemObjWriter<T> : IObjWriter<T>
    {
        void IObjWriter<T>.Serialize(IBufferWriter<byte> writer, T obj) 
            => throw new NotImplementedException();
    }
}