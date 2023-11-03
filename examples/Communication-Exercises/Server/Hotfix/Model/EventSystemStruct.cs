using Fantasy;
using UnityEngine;

namespace BestGame;
public class EventSystemStruct
{
    public struct StartMove
    {
        public Unit Unit;
        public MoveInfo MoveInfo;
        public StartMove()
        {
            Unit = null;
            MoveInfo = null;
        }
    }
}
