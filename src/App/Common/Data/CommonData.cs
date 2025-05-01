using MemoryPack;
using Shared.Tp.Data.Mem;
using Shared.Tp.Data.Mem.Formatters;

namespace Common.Data
{
    public static class CommonData
    {
        private static bool _initialized;
        public static void Initialize()
        {
            if (_initialized)
                return;
            _initialized = true;

#if UNITY_5_6_OR_NEWER
            // just to check unity includes MemoryPack source generation
            ServerState.RegisterFormatter();
#endif

            MemoryPackFormatterProvider.Register(new MemStCmdFormatter<ClientState>());
            MemoryPackFormatterProvider.Register(new MemStCmdFormatter<ServerState>());
        }
    }
}