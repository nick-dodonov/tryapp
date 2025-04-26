// ReSharper disable UnusedMember.Global
// ReSharper disable UnassignedField.Global
// ReSharper disable NotAccessedField.Global

using MemoryPack;

namespace Common.Logic
{
    /// <summary>
    /// Client/server state stub
    /// TODO: try mempack in webgl
    /// TODO: state diff support
    /// TODO: derived support 
    /// </summary>
    //[MemoryPackable]
    public partial struct ClientState
    {
        public int Frame;
        public int Ms;

        public float X;
        public float Y;
        public uint Color;
    }

    // [MemoryPackable]
    public partial struct PeerState
    {
        public string Id;
        public ClientState ClientState;
    }

    // [MemoryPackable]
    public partial struct ServerState
    {
        public int Frame;
        public int Ms;

        public PeerState[] Peers;
    }
}