using Shared.Session;
using UnityEngine;
using UnityEngine.UI;

public class PeerTap : MonoBehaviour
{
    public Image image;

    public void Apply(in ClientState state)
    {
        transform.position = new(state.X, state.Y);
        
        // Convert back from uint to Color32 and assign it to image.color
        var color = state.Color;
        image.color = new Color32((byte)((color >> 16) & 0xFF), (byte)((color >> 8) & 0xFF), (byte)(color & 0xFF), 0xFF);
    }
}
