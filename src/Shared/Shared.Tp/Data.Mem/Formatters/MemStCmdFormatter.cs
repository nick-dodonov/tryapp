using System.Runtime.CompilerServices;
using MemoryPack;
using MemoryPack.Internal;
using Shared.Tp.St.Sync;

#pragma warning disable CS9074 // UNITY: The 'scoped' modifier of parameter doesn't match overridden or implemented member.

namespace Shared.Tp.Data.Mem.Formatters
{
    [Preserve]
    public sealed class MemStCmdFormatter<T> : MemoryPackFormatter<StCmd<T>>
    {
        [Preserve]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer,
#if !UNITY_2020_1_OR_NEWER
            scoped
#endif
                ref StCmd<T> value)
        {
            if (!RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            {
                writer.DangerousWriteUnmanaged(value);
                return;
            }
            writer.WriteVarInt(value.Frame);
            writer.WriteValue(value.Value);
        }

        [Preserve]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void Deserialize(ref MemoryPackReader reader,
#if !UNITY_2020_1_OR_NEWER
            scoped
#endif
                ref StCmd<T> value)
        {
            if (!RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            {
                reader.DangerousReadUnmanaged(out value);
                return;
            }

            value.Frame = reader.ReadVarIntInt32();
            reader.ReadValue(ref value.Value!);
        }
    }
}