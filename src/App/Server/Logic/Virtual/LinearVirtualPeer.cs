using System.Numerics;
using Common.Data;

namespace Server.Logic.Virtual;

public class LinearVirtualPeer(string id, uint color, Vector2 initPos, Vector2 initSpeed) 
    : IVirtualPeer
{
    private Vector2 _pos = initPos;
    private Vector2 _speed = initSpeed;

    PeerState IVirtualPeer.GetPeerState(int sessionMs)
    {
        return new()
        {
            Id = id,
            ClientState = new()
            {
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
        if (_pos.X < -1 && _speed.X < 0 ||
            _pos.X > 1 && _speed.X > 0) 
            _speed.X *= -1;
        if (_pos.Y < -1 && _speed.Y < 0 ||
            _pos.Y > 1 && _speed.Y > 0) 
            _speed.Y *= -1;
    }
}