using System.Buffers;

namespace Shared.Tp.St
{
    public interface IObjWriter<in T>
    {
        void Serialize(IBufferWriter<byte> writer, T obj);
    }
}