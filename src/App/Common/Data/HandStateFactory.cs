using Shared.Tp.Data;
using Shared.Tp.Data.Web;

namespace Common.Data
{
    public static class HandStateFactory
    {
        public static IOwnWriter CreateOwnWriter<T>(T state)
            => new OwnWriter<T>(state, new WebObjWriter<T>());

        public static IObjReader<T> CreateObjReader<T>()
            => new WebObjReader<T>();
    }
}