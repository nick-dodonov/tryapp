using System.Buffers;

namespace Shared.Tp.St
{
    public interface IOwnWriter
    {
        int Serialize(IBufferWriter<byte> writer);
    }
}