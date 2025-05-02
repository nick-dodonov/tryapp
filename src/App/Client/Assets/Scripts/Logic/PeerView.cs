using Common.Data;
using Shared.Tp.St.Sync;
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

        public LineRenderer lineRenderer;

        private bool _changed;
        public bool Changed => _changed;
        public void SetChanged(bool changed) => _changed = changed;

        private PeerState _peerState;

        private float _applySessionMs;
        private Color _applyColor;

        public void ApplyState(in PeerState peerState, History<int, ServerState> history)
        {
            _peerState = peerState;
            ApplyHistory(history);

            ref var clientState = ref _peerState.ClientState;
            transform.position = clientState.LoadPosition();

            // Convert back from uint to Color32 and assign it to image.color
            var colorU = clientState.Color;
            var color32 = new Color32(
                (byte)((colorU >> 16) & 0xFF),
                (byte)((colorU >> 8) & 0xFF),
                (byte)(colorU & 0xFF),
                0xFF);
            _applyColor = color32;
            _applySessionMs = clientState.Ms;
            ApplyColor();

            idText.text = peerState.Id;

            _changed = true;
        }

        private void ApplyHistory(History<int, ServerState> history)
        {
            var index = 0;
            lineRenderer.positionCount = 0;
            foreach (ref var serverState in history.ReverseRefValues)
            {
                if (!serverState.TryGetPeerStateIndex(_peerState.Id, out var peerIndex))
                    continue;

                ref var peerState = ref serverState.Peers[peerIndex];
                lineRenderer.positionCount = index + 1;
                lineRenderer.SetPosition(index++, peerState.ClientState.LoadPosition());
            }
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

    public static class ServerStateExtensions
    {
        //TODO: cache Peers by Id in ServerState
        public static bool TryGetPeerStateIndex(this in ServerState serverState, string peerId, out int peerIndex)
        {
            peerIndex = -1;

            var peers = serverState.Peers;
            for (var i = 0; i < peers.Length; ++i)
            {
                if (peers[i].Id != peerId)
                    continue;

                peerIndex = i;
                return true;
            }

            return false;
        }
    }
}