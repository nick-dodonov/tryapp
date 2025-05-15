namespace Shared.Tp.Tests.Tween
{
    public struct UnmanagedComplex
    {
        public long Offset;
        public UnmanagedBasic UnmanagedBasic;

        public static UnmanagedComplex Make(int idx)
        {
            return new()
            {
                UnmanagedBasic = UnmanagedBasic.Make(idx),
            };
        }
    }
}