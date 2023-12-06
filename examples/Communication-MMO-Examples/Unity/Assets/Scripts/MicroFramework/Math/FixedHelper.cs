using UnityEngine;
using System;

public static class FixedHelper
{
    public static FixedPoint ToFixedPoint(this float value){
        return FixedPoint.FromFloat(value);
    }

    public static FixedPoint ToFixedPoint(this int value){
        return FixedPoint.FromInt(value);
    }

    public static FixedVector2 ToFixedVector2(this Vector2 vector){
        return new FixedVector2(vector.x.ToFixedPoint(), vector.y.ToFixedPoint());
    }

    public static FixedVector3 ToFixedVector3(this Vector3 vector){
        return new FixedVector3(vector.x.ToFixedPoint(), vector.y.ToFixedPoint(), vector.z.ToFixedPoint());
    }

    public static Vector2 ToVector2(this FixedVector2 vector){
        return new Vector2(vector.X.ToFloat(), vector.Y.ToFloat());
    }

    public static Vector3 ToVector3(this FixedVector3 vector){
        return new Vector3(vector.X.ToFloat(), vector.Y.ToFloat(), vector.Z.ToFloat());
    }

    public static Quaternion ToQuaternion(this FixedQuaternion quaternion){
        return new Quaternion(quaternion.X.ToFloat(), quaternion.Y.ToFloat(), quaternion.Z.ToFloat(), quaternion.W.ToFloat());
    }

    public static FixedQuaternion ToFixedQuaternion(this Quaternion quaternion){
        return new FixedQuaternion(quaternion.x.ToFixedPoint(), quaternion.y.ToFixedPoint(), quaternion.z.ToFixedPoint(), quaternion.w.ToFixedPoint());
    }

    public static FixedPoint ToFixedPoint(this Vector3 vector){
        return FixedPoint.FromFloat(vector.magnitude);
    }

    public static FixedPoint ToFixedPoint(this Vector2 vector){
        return FixedPoint.FromFloat(vector.magnitude);
    }

    public static FixedPoint ToFixedPoint(this Vector3 vector, Vector3 other){
        return FixedPoint.FromFloat(Vector3.Distance(vector, other));
    }

    public static FixedPoint ToFixedPoint(this Vector2 vector, Vector2 other){
        return FixedPoint.FromFloat(Vector2.Distance(vector, other));
    }

    public static FixedPoint ToFixedPoint(this Vector3 vector, FixedVector3 other){
        return FixedPoint.FromFloat(Vector3.Distance(vector, other.ToVector3()));
    }

    public static FixedPoint ToFixedPoint(this Vector2 vector, FixedVector2 other){
        return FixedPoint.FromFloat(Vector2.Distance(vector, other.ToVector2()));
    }

    public static FixedPoint ToFixedPoint(this FixedVector3 vector, Vector3 other){
        return FixedPoint.FromFloat(Vector3.Distance(vector.ToVector3(), other));
    }

    public static FixedPoint ToFixedPoint(this FixedVector2 vector, Vector2 other){
        return FixedPoint.FromFloat(Vector2.Distance(vector.ToVector2(), other));
    }
}