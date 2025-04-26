using System.Buffers;

namespace Shared.Tp.Data
{
    public interface IOwnWriter
    {
        void Serialize(IBufferWriter<byte> writer);
    }
}