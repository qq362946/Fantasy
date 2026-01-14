

using UnityEngine;

namespace Fantasy
{
    public struct OnUnitMoveStateChange
    {
        public Unit Unit;
        public Vector3 Position;
        public UnitMoveState State;

        public OnUnitMoveStateChange(Unit unit, UnitMoveState state)
        {
            Unit = unit;
            State = state;
            Position = Vector3.zero;
        }

        public OnUnitMoveStateChange(Unit unit, ref Vector3 position, UnitMoveState state)
        {
            Unit = unit;
            Position  = position;
            State = state;
        }
    }
}