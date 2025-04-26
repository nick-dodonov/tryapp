using System;
using System.Buffers;
using Shared.Web;

namespace Shared.Tp.Data.Web
{
    public class WebObjWriter<T> : IObjWriter<T>
    {
        void IObjWriter<T>.Serialize(IBufferWriter<byte> writer, T obj)
            => WebSerializer.Default.Serialize(writer, obj);
    }

    public class WebObjReader<T> : IObjReader<T>
    {
        T IObjReader<T>.Deserialize(ReadOnlySpan<byte> span)
            => WebSerializer.Default.Deserialize<T>(span);
    }
}