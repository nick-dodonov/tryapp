using Shared.Tp.Data;
using Shared.Tp.Data.Web;

namespace Common.Data
{
    public static class TickStateFactory
    {
        public static IObjWriter<T> CreateObjWriter<T>()
            => new WebObjWriter<T>();
        public static IObjReader<T> CreateObjReader<T>()
            => new WebObjReader<T>();
    }
}