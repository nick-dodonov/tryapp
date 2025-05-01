using Common.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Client.Logic
{
    public class PeerView : MonoBehaviour
    {
        public Image image;
        public MeshRenderer meshRenderer;
        public TMP_Text idText;

        private bool _changed;
        public bool Changed => _changed;
        public void SetChanged(bool changed) => _changed = changed;

        private float _applySessionMs;
        private Color _applyColor;

        public void ApplyState(in PeerState peerState)
        {
            var state = peerState.ClientState;
            _applySessionMs = state.Ms;
            transform.position = state.LoadPosition();

            // Convert back from uint to Color32 and assign it to image.color
            var colorU = state.Color;
            var color32 = new Color32(
                (byte)((colorU >> 16) & 0xFF),
                (byte)((colorU >> 8) & 0xFF),
                (byte)(colorU & 0xFF),
                0xFF);
            _applyColor = color32;

            ApplyColor();
            
            idText.text = peerState.Id;

            _changed = true;
        }

        private const float FadeAlphaMin = 0.1f;
        private const float FadeAlphaSec = 4.0f;

        public void UpdateSessionMs(int sessionMs)
        {
            var t = (sessionMs - _applySessionMs) / FadeAlphaSec / 1000.0f;
            var alpha = Mathf.Lerp(1, FadeAlphaMin, t);
            _applyColor.a = alpha;

            ApplyColor();
        }
        
        private void ApplyColor()
        {
            image.color = _applyColor;
            meshRenderer.material.color = _applyColor;
        }
    }
}