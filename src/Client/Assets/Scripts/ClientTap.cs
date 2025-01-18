using Common.Logic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Client
{
    public class ClientTap : MonoBehaviour
    {
        public Image image;

        private InputAction _clickAction;
        private InputAction _pointAction;

        private uint _clientColor;

        private void Start()
        {
            _clickAction = InputSystem.actions.FindAction("Player/TipPanel"); //("UI/Click");
            _pointAction = InputSystem.actions.FindAction("UI/Point");
        }

        private void OnEnable()
        {
            var rect = Screen.safeArea;
            transform.position = new(
                Random.Range(rect.xMin, rect.xMax),
                Random.Range(rect.yMin, rect.yMax));

            //make random color distinctive with outline (never pure white or too dark) 
            var color = Random.ColorHSV(
                0.0f, 1.0f, //hue (color)
                0.3f, 1.0f, //saturation (white->color)
                0.4f, 1.0f //value (black->color)
            );
            image.color = color;

            var color32 = (Color32)color;
            _clientColor = (uint)((color32.r << 16) | (color32.g << 8) | color32.b);
        }

        private void Update()
        {
            if (_clickAction.IsPressed())
            {
                var pointValue = _pointAction.ReadValue<Vector2>();
                //var cam = Camera.main;
                if (Screen.safeArea.Contains(pointValue)) // && cam != null)
                {
                    //Shared.Log.Slog.Info($"{pointValue.x}, {pointValue.y}");
                    //var ray = cam.ScreenPointToRay(pointValue);
                    transform.position = pointValue;
                }
            }
        }

        public void SetActive(bool active) => gameObject.SetActive(active);

        public void Fill(ref ClientState state)
        {
            var position = transform.position;
            state.X = position.x;
            state.Y = position.y;
            state.Color = _clientColor;
        }
    }
}