using Shared.Tp.Data;
using Shared.Tp.Data.Mem;

namespace Common.Data
{
    public static class TickStateFactory
    {
        public static IObjWriter<T> CreateObjWriter<T>()
            => new MemObjWriter<T>();

        public static IObjReader<T> CreateObjReader<T>()
            => new MemObjReader<T>();
    }
}