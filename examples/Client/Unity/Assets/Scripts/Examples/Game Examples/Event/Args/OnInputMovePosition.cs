using UnityEngine;

namespace Fantasy
{
    public struct OnInputMovePosition
    {
        public Unit Unit;
        public Vector3 Position;

        public OnInputMovePosition(Unit unit, ref Vector3 position)
        {
            Unit = unit;
            Position = position;
        }
    }
}