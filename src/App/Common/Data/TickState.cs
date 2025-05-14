// ReSharper disable UnusedMember.Global
// ReSharper disable UnassignedField.Global
// ReSharper disable NotAccessedField.Global

using MemoryPack;
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
        public float X;
        public float Y;
        public uint Color;
    }

    [MemoryPackable]
    public partial struct PeerState
    {
        [QuickInternStringFormatter]
        public string Id;
        public int Ms; // state fill time (current for virtual peers)

        public ClientState ClientState;
    }

    [MemoryPackable]
    public partial struct ServerState
    {
        public PeerState[] Peers;
    }
}