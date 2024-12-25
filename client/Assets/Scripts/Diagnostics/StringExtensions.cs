namespace Diagnostics
{
    public static class StringExtensions
    {
        public static string ToHumanReadableContent(this string str)
        {
            if (str == null) return "<null>";
            return string.IsNullOrWhiteSpace(str) ? $"'{str}'" : str;
        }
    }
}