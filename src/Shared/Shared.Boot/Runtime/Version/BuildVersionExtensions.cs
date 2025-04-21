using System;
using Cysharp.Text;

namespace Shared.Boot.Version
{
    public static class BuildVersionExtensions
    {
        public static string ToShortInfo(this BuildVersion version)
        {
            var sb = ZString.CreateStringBuilder(true);
            try
            {
                sb.AppendShortInfo(version);
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
        public static void AppendShortInfo(this ref Utf16ValueStringBuilder sb, in BuildVersion version)
        {
            sb.Append(version.Branch);

            sb.Append(" | ");
            var sha = version.Sha.AsSpan();
            sb.Append(sha.Length <= 7 
                ? sha 
                : sha[..7]);

            sb.Append(" | ");
            var isToday = version.Time.Date == DateTime.Today;
            sb.Append(version.Time, isToday 
                ? "HH:mm:sszz" 
                : "yyyy-MM-dd HH:mm:sszz");
        }
    }
}