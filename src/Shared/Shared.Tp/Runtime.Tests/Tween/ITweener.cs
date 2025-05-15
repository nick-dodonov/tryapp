namespace Shared.Tp.Tests.Tween
{
    public interface ITweener { }

    public interface ITweener<TField> : ITweener
    {
        void Process(ref TField a, ref TField b, float t, ref TField r);
    }
}