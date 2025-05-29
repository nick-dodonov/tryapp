using Common.Data;

namespace Server.Logic.Virtual;

public class CircleVirtualPeer(
    string id, uint color,
    float initRadians, float radius, int circleTimeMs, int direction) 
    : IVirtualPeer
{
    private float _sessionMs;

    PeerState IVirtualPeer.GetPeerState(ushort cycledRt)
    {
        var angle = (float)(2 * Math.PI * ((int)(1000*_sessionMs) % circleTimeMs) / circleTimeMs) * direction;
        angle += initRadians;
        var x = -radius * MathF.Cos(angle);
        var y = radius * MathF.Sin(angle);
        return new()
        {
            Id = id,
            CycledRt = cycledRt,
            ClientState = new()
            {
                X = x,
                Y = y,
                Color = color
            }
        };
    }

    void IVirtualPeer.Update(float deltaTime)
    {
        _sessionMs += deltaTime;
    }
}