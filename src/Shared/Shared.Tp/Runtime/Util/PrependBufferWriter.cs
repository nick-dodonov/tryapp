using System;
using System.Buffers;
using Shared.Log;

namespace Shared.Tp.Util
{
    /// <summary>
    /// Wrapper allowing reserve memory and writing it after further advances.
    /// For example, it allows prepending written memory with its size or checksum.
    /// </summary>
    public class PrependBufferWriter : IBufferWriter<byte>, IDisposable
    {
        [ThreadStatic] 
        private static PrependBufferWriter? _cachedWriter;

        public static PrependBufferWriter Rent(IBufferWriter<byte> hostWriter, int reservedCount)
        {
            PrependBufferWriter writer;
            if (_cachedWriter != null)
            {
                writer = _cachedWriter;
                _cachedWriter = null;
                writer.Reinit(hostWriter, reservedCount);
            }
            else
                writer = new(hostWriter, reservedCount);

            return writer;
        }

        private static void InternalReturn(PrependBufferWriter writer)
        {
            if (_cachedWriter == null)
                _cachedWriter = writer;
            else
                Slog.Warn("TODO: implement nested rents caching");
        }

        private IBufferWriter<byte>? _hostWriter;
        private int _reservedCount;
        private Memory<byte> _memory;

        public int ReservedCount => _reservedCount;
        public Memory<byte> ReservedMemory => _memory[.._reservedCount];
        public Span<byte> ReservedSpan => _memory[.._reservedCount].Span;
        //TODO: also provide ReservedWriter as FixedMemoryWriter allowing to check reserved memory is filled

        private int _advancedCount;
        public int WrittenCount => _advancedCount;
        public ReadOnlyMemory<byte> WrittenMemory => _memory.Slice(_reservedCount, _advancedCount);
        public ReadOnlySpan<byte> WrittenSpan => _memory.Slice(_reservedCount, _advancedCount).Span;

        private PrependBufferWriter(IBufferWriter<byte> hostWriter, int reservedCount) 
            => Reinit(hostWriter, reservedCount);

        private void Reinit(IBufferWriter<byte> hostWriter, int reservedCount)
        {
            _hostWriter = hostWriter;
            _reservedCount = reservedCount;
            _memory = _hostWriter.GetMemory(reservedCount);
            _advancedCount = 0;
        }

        private void Reset()
        {
            _hostWriter = null;
            _memory = new();
            _reservedCount = _advancedCount = 0;
        }

        public void Dispose()
        {
            _hostWriter!.Advance(_reservedCount + _advancedCount);
            Reset();
            InternalReturn(this);
        }

        public void Advance(int count)
        {
            _advancedCount += count;
        }

        public Memory<byte> GetMemory(int sizeHint = 0)
        {
            var writingCount = _reservedCount + _advancedCount;
            var availableCount = _memory.Length - writingCount;
            if (availableCount > sizeHint) // '>' (without equal) to handle the case sizeHint=0 that usually means "at least 1"
                return _memory[writingCount..];

            // store and apply reserved + already advanced because host resize keeps only its advanced
            //  (but we don't move it until reserved isn't filled)
            Span<byte> writingSpan = stackalloc byte[writingCount];
            _memory[..writingCount].Span.CopyTo(writingSpan);
            _memory = _hostWriter!.GetMemory(writingCount + sizeHint);
            writingSpan.CopyTo(_memory.Span);
            return _memory[writingCount..];
        }

        public Span<byte> GetSpan(int sizeHint = 0) 
            => GetMemory(sizeHint).Span;
    }
}