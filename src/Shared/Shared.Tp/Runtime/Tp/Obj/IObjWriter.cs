using System.Buffers;

namespace Shared.Tp.Obj
{
    public interface IObjWriter<in T>
    {
        void Serialize(IBufferWriter<byte> writer, T obj);
    }
}