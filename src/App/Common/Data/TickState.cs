// ReSharper disable UnusedMember.Global
// ReSharper disable UnassignedField.Global
// ReSharper disable NotAccessedField.Global

using MemoryPack;
using Shared.Tp.Data.Mem;
using Shared.Tp.Data.Mem.Formatters;

namespace Common.Data
{
    /// <summary>
    /// Client/server state stub
    /// TODO: state diff support
    /// 
    /// </summary>
    [MemoryPackable]
    public partial struct ClientState
    {
        public int Ms;

        public float X;
        public float Y;
        public uint Color;
    }

    [MemoryPackable]
    public partial struct PeerState
    {
        [QuickInternStringFormatter]
        public string Id;

        public ClientState ClientState;
    }

    [MemoryPackable]
    public partial struct ServerState
    {
        public int Ms;

        public PeerState[] Peers;
    }
}