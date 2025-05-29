using Common.Data;

namespace Server.Logic;

public interface IVirtualPeer
{
    public PeerState GetPeerState(ushort cycledRt);
    public void Update(float deltaTime);
}