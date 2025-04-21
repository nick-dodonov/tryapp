using System;
using Cysharp.Text;

namespace Shared.Boot.Version
{
    public static class BuildVersionExtensions
    {
        public static string GetShortDescription(this BuildVersion version)
        {
            var sb = ZString.CreateStringBuilder(true);
            try
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

                return sb.ToString();
            }
            finally
            {
                sb.Dispose();
            }
        }
    }
}