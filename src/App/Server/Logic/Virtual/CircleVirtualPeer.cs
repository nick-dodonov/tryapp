using Common.Logic;

namespace Server.Logic.Virtual;

public class CircleVirtualPeer(
    string id, uint color,
    float initRadians, float radius, int circleTimeMs, int direction) 
    : IVirtualPeer
{
    PeerState IVirtualPeer.GetPeerState(int frame, int sessionMs)
    {
        var angle = (float)(2 * Math.PI * (sessionMs % circleTimeMs) / circleTimeMs) * direction;
        angle += initRadians;
        var x = -radius * MathF.Cos(angle);
        var y = radius * MathF.Sin(angle);
        return new()
        {
            Id = id,
            ClientState = new()
            {
                Frame = frame,
                Ms = sessionMs,
                X = x,
                Y = y,
                Color = color
            }
        };
    }

    void IVirtualPeer.Update(float deltaTime) { }
}