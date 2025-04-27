using Common.Data;
using UnityEngine;

namespace Client.Logic
{
    /// <summary>
    /// Helper for current coordinate system ([-1,1] box to simplify diagnostics and custom impls)
    /// </summary>
    public static class StateExtensions
    {
        public static void UpdatePosition(this ref ClientState state, Transform transform)
        {
            var position = transform.position;
            state.X = position.x / Screen.width * 2 - 1;
            state.Y = position.y / Screen.height * 2 - 1;
        }

        public static void AssignPosition(this in ClientState state, Transform transform)
        {
            transform.position = new(
                (state.X + 1) * Screen.width / 2, 
                (state.Y + 1) * Screen.height / 2);
        }
    }
}