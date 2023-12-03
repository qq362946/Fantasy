using System;
using UnityEngine;

namespace Fantasy
{
    public static class MoveMessageHelper
    {
        public static Vector3 ToVector3(this Position pos)
        {
            Vector3 p = new Vector3(pos.X,pos.Y,pos.Z);
            return p;
        }

        public static Quaternion ToQuaternion(this Rotation rotation)
        {
            Quaternion r = new Quaternion(rotation.RotA,rotation.RotB,rotation.RotC,rotation.RotW);
            return r;
        }

        public static MoveInfo MoveInfo(Vector3 pos = default,Quaternion quat = default)
        {
            MoveInfo m = new Fantasy.MoveInfo();
            m.Position = pos.ToPosition();
            m.Rotation = quat.ToRotation();
            return m;
        }

        public static Position ToPosition(this Vector3 position)
        {
            Position p = new Position
            {
                X = position.x,
                Y = position.y,
                Z = position.z,
            };
            return p;
        }
        public static Rotation ToRotation(this Quaternion quat)
        {
            Rotation r = new Rotation
            {
                RotA = quat.x,
                RotB = quat.y,
                RotC = quat.z,
                RotW = quat.w,
            };
            return r;
        }

        public static MoveState ToMoveState(this int stateInfo)
        {
            return (MoveState)Enum.Parse(typeof(MoveState), stateInfo.ToString());
        }
    }

    public struct UnitState
    {
        public int MoveState;
        public Vector3 Position; 
        public Quaternion Rotation;
        public float MoveSpeed;
        public UnitState(int state, Vector3 position, 
                    Quaternion rotation,float speed)
        {
            this.MoveState = state;
            this.Position = position;
            this.Rotation = rotation;
            this.MoveSpeed = speed;
        }
    }
    public enum MoveState: ushort
    { 
        IDLE = 0, 
        RUNNING = 1, 
        AIRBORNE = 2, 
        SWIMMING = 3, 
        JUMP = 4 , 
        SWIMMINGIDLE = 5 , 
        MOUNTED = 6, 
        MOUNTED_AIRBORNE = 7, 
        MOUNTED_SWIMMING = 8, 
        DEAD = 9, 
    }
}
