using System.Buffers;

namespace Shared.Tp.St
{
    public interface IOwnWriter
    {
        void Serialize(IBufferWriter<byte> writer);
    }
}