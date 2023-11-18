using Fantasy;
using UnityEngine;

namespace BestGame
{
    public static class MessageInfoHelper
    {
        public static Vector3 Vector3(MoveInfo moveInfo)
        {
            Vector3 p = new Vector3(moveInfo.X,moveInfo.Y,moveInfo.Z);
            return p;
        }

        public static Quaternion Quaternion(MoveInfo moveInfo)
        {
            Quaternion r = new Quaternion(moveInfo.RotA,moveInfo.RotB,moveInfo.RotC,moveInfo.RotW);
            return r;
        }

        public static MoveInfo MoveInfo(Vector3 position = default,Quaternion quaternion = default)
        {
            MoveInfo m = new MoveInfo{
                X = position.x,
                Y = position.y,
                Z = position.z,
                RotA = quaternion.x,
                RotB = quaternion.y,
                RotC = quaternion.z,
                RotW = quaternion.w
            };
            return m;
        }
    }
}
