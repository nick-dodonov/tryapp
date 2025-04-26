using System.Buffers;

namespace Shared.Tp.Data
{
    public interface IObjWriter<in T>
    {
        void Serialize(IBufferWriter<byte> writer, T obj);
    }
}