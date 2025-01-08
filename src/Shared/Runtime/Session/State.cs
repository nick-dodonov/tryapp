namespace Shared.Session
{
    /// <summary>
    /// Client/server state stub
    /// TODO: try mempack in webgl
    /// TODO: state diff support
    /// TODO: derived support 
    /// </summary>
    public struct ClientState
    {
        public int Frame;
        public long UtcMs;

        public float X;
        public float Y;
        public uint Color;
    }

    public struct PeerState
    {
        public string Id;
        public ClientState ClientState;
    }

    public struct ServerState
    {
        public int Frame;
        public long UtcMs;

        public PeerState[] Peers;
    }
}