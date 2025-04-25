using System;
using System.Buffers;
using System.Diagnostics;
using Shared.Web;

namespace Shared.Tp.St.Std
{
    public class StdOwnWriter<T> : IOwnWriter
    {
        private readonly T _obj;

        public StdOwnWriter(T obj)
        {
            Debug.Assert(obj != null);
            _obj = obj;
        }

        public override string ToString() => _obj!.ToString();

        void IOwnWriter.Serialize(IBufferWriter<byte> writer) 
            => WebSerializer.Default.Serialize(writer, _obj);
    }

    public class StdObjReader<T> : IObjReader<T>
    {
        T IObjReader<T>.Deserialize(ReadOnlySpan<byte> span)
            => WebSerializer.Default.Deserialize<T>(span);
    }

    public class StdObjWriter<T> : IObjWriter<T>
    {
        void IObjWriter<T>.Serialize(IBufferWriter<byte> writer, T obj) 
            => WebSerializer.Default.Serialize(writer, obj);
    }
}