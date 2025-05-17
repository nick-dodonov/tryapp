using MemoryPack;
using MemoryPack.Internal;
using Shared.Sys.Rtt;
using Shared.Tp.Data.Mem.Formatters;
using Shared.Tp.Tween;

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
        //TODO: use Vector2 shim
        [Tween]
        public float X;
        [Tween]
        public float Y;

        public uint Color;
    }

    [MemoryPackable]
    public partial struct PeerState
    {
        [QuickInternStringFormatter]
        public string Id;
        
        [Tween]
        public int Ms; // state fill time (current for virtual peers), used only for local visual diagnostics of network latency

        [Tween]
        public ClientState ClientState;

        [Preserve] // ReSharper disable once UnusedMember.Global // TODO: auto-create via Shared.Sys.SourceGen
        public RttInfo GetRttInfo() => new RttInfo()
            .Add(ref this, ref Id, nameof(Id))
            .Add(ref this, ref Ms, nameof(Ms))
            .Add(ref this, ref ClientState, nameof(ClientState))
        ;
    }

    [MemoryPackable]
    public partial struct ServerState
    {
        [Tween]
        public PeerState[] Peers;

        [Preserve] // ReSharper disable once UnusedMember.Global // TODO: auto-create via Shared.Sys.SourceGen
        public RttInfo GetRttInfo() => new RttInfo()
            .Add(ref this, ref Peers, nameof(Peers))
        ;
    }
}