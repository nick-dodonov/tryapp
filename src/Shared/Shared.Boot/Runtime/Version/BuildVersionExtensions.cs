using System;
using Cysharp.Text;

namespace Shared.Boot.Version
{
    public static class BuildVersionExtensions
    {
        public static string ToShortInfo(this BuildVersion version, bool shortToday = false)
        {
            var sb = ZString.CreateStringBuilder(true);
            try
            {
                sb.AppendShortInfo(version, shortToday);
                return sb.ToString();
            }
            finally
            {
                sb.Dispose();
            }
        }
    }

    public static class ZStringExtensions
    {
        public static void AppendShortInfo(this ref Utf16ValueStringBuilder sb, in BuildVersion version, bool shortToday = false)
        {
            sb.Append(version.Branch);

            sb.Append(" | ");
            var sha = version.Sha.AsSpan();
            sb.Append(sha.Length <= 7 
                ? sha 
                : sha[..7]);

            sb.Append(" | ");
            var timeFormat = shortToday
                ? (version.Time.Date == DateTime.Today)
                    ? "HH:mm:sszz"
                    : "yyyy-MM-dd"
                : "yyyy-MM-dd HH:mm:sszz";
            sb.Append(version.Time, timeFormat);
        }
    }
}