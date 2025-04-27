using MemoryPack;
using MemoryPack.Internal;
using Shared.Tp.St.Sync;

namespace Shared.Tp.Data.Mem
{
    [Preserve]
    public sealed class MemStCmdFormatter<T> : MemoryPackFormatter<StCmd<T>>
    {
        [Preserve]
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer,
#if !UNITY_2020_1_OR_NEWER
            scoped
#endif
                ref StCmd<T> value)
        {
            if (!System.Runtime.CompilerServices.RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            {
                writer.DangerousWriteUnmanaged(value);
                return;
            }
            writer.WriteVarInt(value.Frame);
            writer.WriteValue(value.Value);
        }

        [Preserve]
        public override void Deserialize(ref MemoryPackReader reader,
#if !UNITY_2020_1_OR_NEWER
            scoped
#endif
                ref StCmd<T> value)
        {
            if (!System.Runtime.CompilerServices.RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            {
                reader.DangerousReadUnmanaged(out value);
                return;
            }

            value.Frame = reader.ReadVarIntInt32();
            reader.ReadValue(ref value.Value!);
        }
    }
}