using Common.Logic;
using Shared.Tp.Data;
using Shared.Tp.Data.Mem;

namespace Common.Data
{
    public static class TickStateFactory
    {
        public static IObjWriter<T> CreateObjWriter<T>()
            => new MemObjWriter<T>();

        public static IObjReader<T> CreateObjReader<T>()
        {
            //ServerState.RegisterFormatter();
            return new MemObjReader<T>();
        }
    }
}