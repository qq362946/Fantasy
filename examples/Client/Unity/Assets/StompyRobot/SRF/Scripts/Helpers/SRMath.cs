using System;
using UnityEngine;

public static partial class SRMath
{
    /// <summary>
    /// Lerp from one value to another, without clamping t to 0-1.
    /// </summary>
    /// <param name="from"></param>
    /// <param name="to"></param>
    /// <param name="t"></param>
    /// <returns></returns>
    public static float LerpUnclamped(float from, float to, float t)
    {
        return (1.0f - t)*from + t*to;
    }

    /// <summary>
    /// Lerp from one vector to another, without clamping t
    /// </summary>
    /// <param name="from"></param>
    /// <param name="to"></param>
    /// <param name="t"></param>
    /// <returns></returns>
    public static Vector3 LerpUnclamped(Vector3 from, Vector3 to, float t)
    {
        return new Vector3(
            LerpUnclamped(from.x, to.x, t),
            LerpUnclamped(from.y, to.y, t),
            LerpUnclamped(from.z, to.z, t)
            );
    }

    /// <summary>
    /// Value from 0.0f-1.0f, 0 when facing fully away and 1.0f when facing fully towards
    /// </summary>
    public static float FacingNormalized(Vector3 dir1, Vector3 dir2)
    {
        dir1.Normalize();
        dir2.Normalize();

        return Mathf.InverseLerp(-1, 1, Vector3.Dot(dir1, dir2));
    }

    /// <summary>
    /// Reduces a given angle to a value between 180 and -180.
    /// </summary>
    /// <param name="angle">The angle to reduce, in radians.</param>
    /// <returns>The new angle, in radians.</returns>
    /// https://github.com/mono/MonoGame/blob/develop/MonoGame.Framework/MathHelper.cs
    public static float WrapAngle(float angle)
    {
        if (angle <= -180f)
        {
            angle += 360f;
        }
        else
        {
            if (angle > 180f)
            {
                angle -= 360f;
            }
        }
        return angle;
    }

    /// <summary>
    /// Return the angle closest to 'to'
    /// </summary>
    /// <param name="to"></param>
    /// <param name="angle1"></param>
    /// <param name="angle2"></param>
    /// <returns></returns>
    public static float NearestAngle(float to, float angle1, float angle2)
    {
        if (Mathf.Abs(Mathf.DeltaAngle(to, angle1)) > Mathf.Abs(Mathf.DeltaAngle(to, angle2)))
        {
            return angle2;
        }
        return angle1;
    }

    /// <summary>
    /// Wrap value to 0-max (non-inclusive)
    /// </summary>
    /// <param name="max">Max value (non-inclusive)</param>
    /// <param name="value"></param>
    /// <returns>Value wrapped from 0-max</returns>
    public static int Wrap(int max, int value)
    {
        if (max < 0)
        {
            throw new ArgumentOutOfRangeException("max", "max must be greater than 0");
        }

        while (value < 0)
        {
            value += max;
        }

        while (value >= max)
        {
            value -= max;
        }

        return value;
    }

    /// <summary>
    /// Wrap value to 0-max (non-inclusive)
    /// </summary>
    /// <param name="max">Max value (non-inclusive)</param>
    /// <param name="value"></param>
    /// <returns>Value wrapped from 0-max</returns>
    public static float Wrap(float max, float value)
    {
        while (value < 0)
        {
            value += max;
        }

        while (value >= max)
        {
            value -= max;
        }

        return value;
    }

    public static float Average(float v1, float v2)
    {
        return (v1 + v2)*0.5f;
    }

    /// <summary>
    /// Return an angle in range -180, 180 based on direction vector
    /// </summary>
    /// <param name="direction"></param>
    /// <returns></returns>
    public static float Angle(Vector2 direction)
    {
        var angle = Vector3.Angle(Vector3.up, direction);

        if (Vector3.Cross(direction, Vector3.up).z > 0f)
        {
            angle *= -1;
        }

        return angle;
    }
}
