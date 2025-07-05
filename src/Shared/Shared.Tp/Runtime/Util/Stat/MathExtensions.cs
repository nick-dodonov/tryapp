namespace Shared.Tp.Util.Stat
{
    internal static class MathExtensions
    {
        public static float Lerp(float a, float b, float t)
        {
            t = t < 0f ? 0f : (t > 1f ? 1f : t);
            return a + (b - a) * t;
        }
    }
}