using System.Numerics;
using Common.Logic;

namespace Server.Logic.Virtual;

public class LinearVirtualPeer(string id, uint color, Vector2 initPos, Vector2 initSpeed) 
    : IVirtualPeer
{
    private Vector2 _pos = initPos;
    private Vector2 _speed = initSpeed;

    PeerState IVirtualPeer.GetPeerState(int frame, int sessionMs)
    {
        return new()
        {
            Id = id,
            ClientState = new()
            {
                Frame = frame,
                Ms = sessionMs,
                X = _pos.X,
                Y = _pos.Y,
                Color = color
            }
        };
    }

    void IVirtualPeer.Update(float deltaTime)
    {
        _pos += _speed * deltaTime;
        if (_pos.X is < -1 or > 1) _speed.X *= -1;
        if (_pos.Y is < -1 or > 1) _speed.Y *= -1;
    }
}