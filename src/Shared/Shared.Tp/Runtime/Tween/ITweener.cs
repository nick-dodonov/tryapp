namespace Shared.Tp.Tween
{
    public interface ITweener { }

    public interface ITweener<T> : ITweener
    {
        void Replica(ref T dst, in T src);
        void Process(ref T dst, float t, in T src0, in T src1);
    }
}