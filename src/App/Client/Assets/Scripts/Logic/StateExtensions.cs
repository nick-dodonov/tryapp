using Common.Data;
using UnityEngine;

namespace Client.Logic
{
    /// <summary>
    /// Helper for the current coordinate system ([-1,1] box to simplify diagnostics and custom impls)
    /// </summary>
    public static class StateExtensions
    {
        public static void SavePosition(this ref ClientState state, Vector3 position)
        {
            var cam = Camera.main;
            if (cam != null)
            {
                var screenPos = cam.WorldToScreenPoint(position);
                state.X = screenPos.x / Screen.width * 2 - 1;
                state.Y = screenPos.y / Screen.height * 2 - 1;
            }
            else
                state.X = state.Y = 0;
        }

        public static Vector3 LoadPosition(this in ClientState state)
        {
            var cam = Camera.main;
            if (cam != null)
            {
                var screenPos = new Vector3(
                    (state.X + 1) * Screen.width / 2,
                    (state.Y + 1) * Screen.height / 2,
                    0);
                var ray = cam.ScreenPointToRay(screenPos);
                var plane = new Plane(Vector3.forward, Vector3.zero);
                if (plane.Raycast(ray, out var distance))
                {
                    return ray.GetPoint(distance);
                }
            }

            return Vector3.zero;
        }
    }
}