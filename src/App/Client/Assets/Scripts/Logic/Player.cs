using Common.Data;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Client.Logic
{
    public class Player : MonoBehaviour
    {
        public Image image;

        // private Canvas _canvas;
        // private RectTransform _canvasTransform;
        // private RectTransform _imageTransform;

        private InputAction _clickAction;
        private InputAction _pointAction;

        private uint _clientColor;

        // private void Awake()
        // {
        //     _canvas = GetComponentInParent<Canvas>();
        //     if (_canvas != null)
        //     {
        //         _canvasTransform = _canvas.GetComponent<RectTransform>();
        //         _imageTransform = image.GetComponent<RectTransform>();
        //     }
        // }

        private void Start()
        {
            _clickAction = InputSystem.actions.FindAction("Player/TipPanel"); //("UI/Click");
            _pointAction = InputSystem.actions.FindAction("UI/Point");
        }

        private void OnEnable()
        {
            var rect = Screen.safeArea;
            ApplyScreenPos(new(
                Random.Range(rect.xMin, rect.xMax),
                Random.Range(rect.yMin, rect.yMax)));

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
                var screenPos = _pointAction.ReadValue<Vector2>();
                if (Screen.safeArea.Contains(screenPos))
                {
                    //Slog.Info($"screenPos: {screenPos.x}, {screenPos.y}");
                    ApplyScreenPos(screenPos);
                }
            }
        }

        private void ApplyScreenPos(Vector2 screenPos)
        {
            // if (_canvas != null)
            // {
            //     if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            //             _canvasTransform,
            //             screenPos,
            //             _canvas.worldCamera,
            //             out var localPoint))
            //     {
            //         //Slog.Info($"anchoredPosition: {screenPos.x}, {screenPos.y}");
            //         _imageTransform.anchoredPosition = localPoint;
            //     }
            // }

            var cam = Camera.main;
            if (cam != null)
            {
                var ray = cam.ScreenPointToRay(screenPos);
                var plane = new Plane(Vector3.forward, Vector3.zero);
                if (plane.Raycast(ray, out var distance))
                {
                    var worldPoint = ray.GetPoint(distance);
                    transform.position = worldPoint;
                }
            }
        }

        public void Fill(ref ClientState state)
        {
            state.SavePosition(transform.position);
            state.Color = _clientColor;
        }
    }
}