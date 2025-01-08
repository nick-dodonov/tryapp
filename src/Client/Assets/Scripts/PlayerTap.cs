using Shared.Session;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerTap : MonoBehaviour
{
    private InputAction _clickAction;
    private InputAction _pointAction;

    private void Start()
    {
        _clickAction = InputSystem.actions.FindAction("UI/Click");
        _pointAction = InputSystem.actions.FindAction("UI/Point");
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

    public void Fill(ref ClientState msg)
    {
        var position = transform.position;
        msg.X = position.x;
        msg.Y = position.y;
    }
}
