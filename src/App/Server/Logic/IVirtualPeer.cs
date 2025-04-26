using Common.Data;

namespace Server.Logic;

public interface IVirtualPeer
{
    public PeerState GetPeerState(int frame, int sessionMs);
    public void Update(float deltaTime);
}