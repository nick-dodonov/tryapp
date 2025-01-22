using Common.Logic;
using UnityEngine;

namespace Client.Logic
{
    public static class StateExtensions
    {
        public static void UpdatePosition(this ref ClientState state, Transform transform)
        {
            var position = transform.position;
            state.X = position.x;
            state.Y = position.y;
        }

        public static void AssignPosition(this in ClientState state, Transform transform) => 
            transform.position = new(state.X, state.Y);
    }
}