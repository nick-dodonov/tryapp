using Common.Logic;
using UnityEngine;
using UnityEngine.UI;

namespace Client.Logic
{
    public class PeerTap : MonoBehaviour
    {
        public Image image;

        private bool _changed;
        public bool Changed => _changed;
        public void SetChanged(bool changed) => _changed = changed;

        private float _applySessionMs;

        public void Apply(in ClientState state)
        {
            _applySessionMs = state.Ms;
            state.AssignPosition(transform);

            // Convert back from uint to Color32 and assign it to image.color
            var color = state.Color;
            image.color = new Color32(
                (byte)((color >> 16) & 0xFF),
                (byte)((color >> 8) & 0xFF),
                (byte)(color & 0xFF),
                0xFF);

            _changed = true;
        }

        private const float FadeAlphaMin = 0.1f;
        private const float FadeAlphaSec = 4.0f;

        public void UpdateSessionMs(int sessionMs)
        {
            var t = (sessionMs - _applySessionMs) / FadeAlphaSec / 1000.0f;
            var alpha = Mathf.Lerp(1, FadeAlphaMin, t);
            var color = image.color;
            image.color = new(color.r, color.g, color.b, alpha);
        }
    }
}