namespace Shared.Tp.Tween
{
    public interface ITweener { }

    public interface ITweener<TField> : ITweener
    {
        void Process(in TField a, in TField b, float t, ref TField r);
    }
}