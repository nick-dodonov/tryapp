using System;
using System.Buffers;

namespace Shared.Tp
{
    public static class TpLinkExtensions
    {
        private unsafe struct RawSpan
        {
            public byte* Ptr;
            public int Length;
        }

        /// <summary>
        /// Helper to write ReadOnlySpan without C#13 (because callback cannot use ref struct in C#9)
        /// </summary>
        public static unsafe void Send(this ITpLink link, ReadOnlySpan<byte> span)
        {
            fixed (byte* ptr = span)
            {
                link.Send(static (writer, state) =>
                {
                    var span = new ReadOnlySpan<byte>(state.Ptr, state.Length);
                    writer.Write(span);
                }, new RawSpan { Ptr = ptr, Length = span.Length });
            }
        }
    }
}