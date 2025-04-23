using System.Buffers;

namespace Shared.Tp.St
{
    public interface IOwnStateWriter
    {
        int Serialize(IBufferWriter<byte> writer);
    }
}