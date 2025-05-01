using System;
using System.Buffers;
using System.Runtime.CompilerServices;

namespace Shared.Tp.Util
{
    /// <summary>
    /// Wrapper to simplify working with ArrayPool.
    /// It differs from "standard" MemoryPool/MemoryOwner with the absence of GC usage. 
    /// </summary>
    public readonly ref struct SlimMemoryOwner<T>
    {
        private readonly T[] _array;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public SlimMemoryOwner(int minimumBufferSize) 
            => _array = ArrayPool<T>.Shared.Rent(minimumBufferSize);

        public Memory<T> Memory
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _array.AsMemory();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose() 
            => ArrayPool<T>.Shared.Return(_array);
    }

    public sealed class SlimMemoryPool<T>
    {
        private static readonly SlimMemoryPool<T> _shared = new();
        public static SlimMemoryPool<T> Shared => _shared;
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public SlimMemoryOwner<T> Rent(int minimumBufferSize = -1) 
            => new(minimumBufferSize);
    }
}