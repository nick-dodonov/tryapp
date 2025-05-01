using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using MemoryPack;
using MemoryPack.Internal;

#pragma warning disable CS9074 // UNITY: The 'scoped' modifier of parameter doesn't match overridden or implemented member.

namespace Shared.Tp.Data.Mem.Formatters
{
    /// <summary>
    /// GC-free variant of MemoryPack.Formatters.InternStringFormatter
    /// </summary>
    [Preserve]
    public sealed class QuickInternStringFormatter : MemoryPackFormatter<string>
    {
        public static readonly QuickInternStringFormatter Default = new();
        private static readonly Dictionary<ulong, List<string>> _interned = new();

        [Preserve]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer,
#if !UNITY_2020_1_OR_NEWER
            scoped
#endif
            ref string? value)
        {
            writer.WriteString(value);
        }

        /// <summary>
        /// Reimplemented reader.ReadString()
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="value"></param>
        [Preserve]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void Deserialize(ref MemoryPackReader reader,
#if !UNITY_2020_1_OR_NEWER
            scoped
#endif
            ref string? value)
        {
            if (!reader.TryReadCollectionHeader(out var length))
            {
                value = null;
                return;
            }

            if (length == 0)
            {
                value = string.Empty;
                return;
            }

            if (length > 0)
            {
                var byteCount = checked(length * 2);
                ref var src = ref reader.GetSpanReference(byteCount);
                var span = MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<byte, char>(ref src), length);
                UpdateValue(span, ref value);
                reader.Advance(byteCount);
            }
            else
            {
                var utf8Length = length;
                utf8Length = ~utf8Length;
                ref var spanRef = ref reader.GetSpanReference(length + 4);

                var utf16Length = Unsafe.ReadUnaligned<int>(ref spanRef);
                if (utf16Length > 0)
                {
                    var max = unchecked((reader.Remaining + 1) * 3);
                    if (max < 0) max = int.MaxValue;
                    if (max < utf16Length)
                        MemoryPackSerializationException.ThrowInsufficientBufferUnless(utf8Length);
                }

                var src = MemoryMarshal.CreateReadOnlySpan(ref Unsafe.Add(ref spanRef, 4), utf8Length);

                Span<char> chars = stackalloc char[Encoding.UTF8.GetMaxCharCount(utf8Length)];
                var count = Encoding.UTF8.GetChars(src, chars);

                var span = chars[..count];
                UpdateValue(span, ref value);
                reader.Advance(utf8Length + 4);
            }
        }

        //TODO: also support variant value is already set (value.AsSpan().SequenceEqual(span))
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void UpdateValue(ReadOnlySpan<char> span, ref string? value)
        {
            if (value != null)
            {
                var valueSpan = value.AsSpan();
                if (valueSpan.SequenceEqual(span))
                    return;
            }

            value = Intern(span);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static string Intern(ReadOnlySpan<char> span)
        {
            var hash = QuickHashCode(span);
            if (_interned.TryGetValue(hash, out var list))
            {
                foreach (var s in list)
                    if (s.AsSpan().SequenceEqual(span))
                        return s;
            }
            else
                _interned.Add(hash, list = new());

            var str = new string(span);
            list.Add(str);
            return str;
        }

        //TODO: in .NET9 string.GetHashCode(span) can be used instead
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe ulong QuickHashCode(ReadOnlySpan<char> span)
        {
            fixed (char* ptr = span)
            {
                ulong hash = 2166136261;
                const int buckedSize = (sizeof(ulong) / sizeof(char));

                var end = ptr + span.Length;
                var current = ptr;
                while (current + buckedSize <= end)
                {
                    hash = (hash ^ *(ulong*)current) * 16777619;
                    current += buckedSize;
                }

                while (current < end)
                {
                    hash = (hash ^ *current) * 16777619;
                    current++;
                }

                return hash;
            }
        }
    }

    public sealed class QuickInternStringFormatterAttribute
        : MemoryPackCustomFormatterAttribute<QuickInternStringFormatter, string>
    {
        public override QuickInternStringFormatter GetFormatter() => QuickInternStringFormatter.Default;
    }
}