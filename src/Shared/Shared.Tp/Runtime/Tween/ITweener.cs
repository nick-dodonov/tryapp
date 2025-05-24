namespace Shared.Tp.Tween
{
    public interface ITweener { }

    public interface ITweener<T> : ITweener
    {
        void Process(in T src0, in T src1, float t, ref T dst);
    }
}