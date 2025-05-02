using Common.Data;
using Shared.Tp.St.Sync;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Client.Logic
{
    public interface IViewHandler
    {
        public int SessionMs { get; }
    }
    
    /// <summary>
    /// TODO: mv interpolation logic to generic and customization
    /// 
    /// </summary>
    public class PeerView : MonoBehaviour
    {
        public Image image;
        public MeshRenderer meshRenderer;
        public TMP_Text idText;

        public LineRenderer lineRenderer;

        private IViewHandler _viewHandler;
        public void SetViewHandler(IViewHandler viewHandler) 
            => _viewHandler = viewHandler;

        private bool _changed;
        public bool Changed => _changed;
        public void SetChanged(bool changed) => _changed = changed;

        private PeerState _peerState;
        private readonly ClientState[] _states = new ClientState[2];

        private Color _applyColor;

        public void ApplyLastState(in PeerState peerState, StHistory<ServerState> history)
        {
            _peerState = peerState;
            ApplyHistory(history);

            ref var clientState = ref _peerState.ClientState;
            var colorU = clientState.Color;
            var color32 = new Color32(
                (byte)((colorU >> 16) & 0xFF),
                (byte)((colorU >> 8) & 0xFF),
                (byte)(colorU & 0xFF),
                0xFF);
            _applyColor = color32;
            ApplyColor();

            idText.text = peerState.Id;

            _changed = true;
            
            Update(); // update according to state (alpha, interpolated pos)
        }

        private void ApplyHistory(StHistory<ServerState> history)
        {
            var index = 0;
            lineRenderer.positionCount = 0;
            foreach (ref var serverState in history.ReverseRefValues)
            {
                if (!serverState.TryGetPeerStateIndex(_peerState.Id, out var peerIndex))
                    continue;

                ref var peerState = ref serverState.Peers[peerIndex];

                if (index < _states.Length)
                    _states[index] = peerState.ClientState;

                lineRenderer.positionCount = index + 1;
                lineRenderer.SetPosition(index++, peerState.ClientState.LoadPosition());
            }

            Debug.Assert(index > 0); // always have the last state
            if (index <= 1)
                _states[1] = _states[0];
        }

        public void ApplyInterpolatedState(in PeerState peerState)
        {
            transform.position = peerState.ClientState.LoadPosition();
        }

        private const float FadeAlphaMin = 0.1f;
        private const float FadeAlphaSec = 4.0f;

        public void Update()
        {
            var sessionMs = _viewHandler.SessionMs;
            var t = (sessionMs - _peerState.Ms) / FadeAlphaSec / 1000.0f;
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