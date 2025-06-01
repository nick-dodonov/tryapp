using Cysharp.Text;

namespace Shared.Tp.St.Sync
{
    public static class StHistoryDebugExtensions
    {
        public static string DebugGetHistoryKeysArrayString<T>(this StHistory<T> history)
        {
            var sb = ZString.CreateStringBuilder(true);
            try
            {
                sb.Append('[');
                var idx = 0;
                foreach (ref var item in history.ReverseRefItems)
                {
                    if (idx++ > 0)
                        sb.Append(", ");
                    sb.Append(item.Key.CycledRt);
                }
                sb.Append(']');
                return sb.ToString();
            }
            finally
            {
                sb.Dispose();
            }
        }
    }
}