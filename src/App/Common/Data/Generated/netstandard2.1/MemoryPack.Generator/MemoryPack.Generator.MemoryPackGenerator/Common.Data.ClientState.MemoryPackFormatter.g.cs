﻿
// <auto-generated/>
#nullable enable
#pragma warning disable CS0108 // hides inherited member
#pragma warning disable CS0162 // Unreachable code
#pragma warning disable CS0164 // This label has not been referenced
#pragma warning disable CS0219 // Variable assigned but never used
#pragma warning disable CS0618 // Type or member is obsolete
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning disable CS8601 // Possible null reference assignment
#pragma warning disable CS8602
#pragma warning disable CS8604 // Possible null reference argument for parameter
#pragma warning disable CS8619
#pragma warning disable CS8620
#pragma warning disable CS8631 // The type cannot be used as type parameter in the generic type or method
#pragma warning disable CS8765 // Nullability of type of parameter
#pragma warning disable CS9074 // The 'scoped' modifier of parameter doesn't match overridden or implemented member
#pragma warning disable CA1050 // Declare types in namespaces.

using System;
using MemoryPack;

namespace Common.Data {

/// <remarks>
/// MemoryPack GenerateType: unmanaged<br/>
/// <code>
/// <b>float</b> X<br/>
/// <b>float</b> Y<br/>
/// <b>uint</b> Color<br/>
/// </code>
/// </remarks>
partial struct ClientState : IMemoryPackable<ClientState>
{

    static partial void StaticConstructor();

    static ClientState()
    {
        RegisterFormatter();
        StaticConstructor();
    }

    [global::MemoryPack.Internal.Preserve]
    public static void RegisterFormatter()
    {
        if (!global::MemoryPack.MemoryPackFormatterProvider.IsRegistered<ClientState>())
        {
            global::MemoryPack.MemoryPackFormatterProvider.Register(new ClientStateFormatter());
        }
        if (!global::MemoryPack.MemoryPackFormatterProvider.IsRegistered<ClientState[]>())
        {
            global::MemoryPack.MemoryPackFormatterProvider.Register(new global::MemoryPack.Formatters.ArrayFormatter<ClientState>());
        }

    }

    [global::MemoryPack.Internal.Preserve]
    public static void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, ref ClientState value) where TBufferWriter : class, System.Buffers.IBufferWriter<byte>
    {

        writer.WriteUnmanaged(value);
    END:

        return;
    }

    [global::MemoryPack.Internal.Preserve]
    public static void Deserialize(ref MemoryPackReader reader, ref ClientState value)
    {

        reader.ReadUnmanaged(out value);
    END:

        return;
    }
}
partial struct ClientState
{
    [global::MemoryPack.Internal.Preserve]
    sealed class ClientStateFormatter : MemoryPackFormatter<ClientState>
    {
        [global::MemoryPack.Internal.Preserve]
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer,  ref ClientState value)
        {
            ClientState.Serialize(ref writer, ref value);
        }

        [global::MemoryPack.Internal.Preserve]
        public override void Deserialize(ref MemoryPackReader reader, ref ClientState value)
        {
            ClientState.Deserialize(ref reader, ref value);
        }
    }
}
}
