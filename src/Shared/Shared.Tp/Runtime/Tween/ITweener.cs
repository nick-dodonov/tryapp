namespace Shared.Tp.Tween
{
    public interface ITweener { }

    public interface ITweener<T> : ITweener
    {
        void Replica(in T src, ref T dst);
        void Process(in T src0, in T src1, float t, ref T dst);
    }
}