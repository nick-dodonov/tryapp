using Shared.Session;
using UnityEngine;

public class PeerTap : MonoBehaviour
{
    public void Apply(in ClientState state)
    {
        transform.position = new(state.X, state.Y);
    }
}
