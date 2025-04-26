using System.Buffers;
using System.Diagnostics;

namespace Shared.Tp.Data
{
    public class OwnWriter<T> : IOwnWriter
    {
        private readonly T _obj;
        private readonly IObjWriter<T> _writer;

        public OwnWriter(T obj, IObjWriter<T> writer)
        {
            Debug.Assert(obj != null);
            _obj = obj;
            _writer = writer;
        }

        public override string ToString() => _obj!.ToString();

        void IOwnWriter.Serialize(IBufferWriter<byte> writer)
            => _writer.Serialize(writer, _obj);
    }
}