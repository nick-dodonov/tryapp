using System.Buffers;

namespace Shared.Tp.Obj
{
    public interface IOwnWriter
    {
        void Serialize(IBufferWriter<byte> writer);
    }
}