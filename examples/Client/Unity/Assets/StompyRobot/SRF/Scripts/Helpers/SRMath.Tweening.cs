using System;
using UnityEngine;

public static partial class SRMath
{
    public enum EaseType
    {
        Linear,
        QuadEaseOut,
        QuadEaseIn,
        QuadEaseInOut,
        QuadEaseOutIn,
        ExpoEaseOut,
        ExpoEaseIn,
        ExpoEaseInOut,
        ExpoEaseOutIn,
        CubicEaseOut,
        CubicEaseIn,
        CubicEaseInOut,
        CubicEaseOutIn,
        QuartEaseOut,
        QuartEaseIn,
        QuartEaseInOut,
        QuartEaseOutIn,
        QuintEaseOut,
        QuintEaseIn,
        QuintEaseInOut,
        QuintEaseOutIn,
        CircEaseOut,
        CircEaseIn,
        CircEaseInOut,
        CircEaseOutIn,
        SineEaseOut,
        SineEaseIn,
        SineEaseInOut,
        SineEaseOutIn,
        ElasticEaseOut,
        ElasticEaseIn,
        ElasticEaseInOut,
        ElasticEaseOutIn,
        BounceEaseOut,
        BounceEaseIn,
        BounceEaseInOut,
        BounceEaseOutIn,
        BackEaseOut,
        BackEaseIn,
        BackEaseInOut,
        BackEaseOutIn
    }

    public static float Ease(float from, float to, float t, EaseType type)
    {
        switch (type)
        {
            case EaseType.Linear:
                return TweenFunctions.Linear(t, from, to, 1f);
            case EaseType.QuadEaseOut:
                return TweenFunctions.QuadEaseOut(t, from, to, 1f);
            case EaseType.QuadEaseIn:
                return TweenFunctions.QuadEaseIn(t, from, to, 1f);
            case EaseType.QuadEaseInOut:
                return TweenFunctions.QuadEaseInOut(t, from, to, 1f);
            case EaseType.QuadEaseOutIn:
                return TweenFunctions.QuadEaseOutIn(t, from, to, 1f);
            case EaseType.ExpoEaseOut:
                return TweenFunctions.ExpoEaseOut(t, from, to, 1f);
            case EaseType.ExpoEaseIn:
                return TweenFunctions.ExpoEaseIn(t, from, to, 1f);
            case EaseType.ExpoEaseInOut:
                return TweenFunctions.ExpoEaseInOut(t, from, to, 1f);
            case EaseType.ExpoEaseOutIn:
                return TweenFunctions.ExpoEaseOutIn(t, from, to, 1f);
            case EaseType.CubicEaseOut:
                return TweenFunctions.CubicEaseOut(t, from, to, 1f);
            case EaseType.CubicEaseIn:
                return TweenFunctions.CubicEaseIn(t, from, to, 1f);
            case EaseType.CubicEaseInOut:
                return TweenFunctions.CubicEaseInOut(t, from, to, 1f);
            case EaseType.CubicEaseOutIn:
                return TweenFunctions.CubicEaseOutIn(t, from, to, 1f);
            case EaseType.QuartEaseOut:
                return TweenFunctions.QuartEaseOut(t, from, to, 1f);
            case EaseType.QuartEaseIn:
                return TweenFunctions.QuartEaseIn(t, from, to, 1f);
            case EaseType.QuartEaseInOut:
                return TweenFunctions.QuartEaseInOut(t, from, to, 1f);
            case EaseType.QuartEaseOutIn:
                return TweenFunctions.QuartEaseOutIn(t, from, to, 1f);
            case EaseType.QuintEaseOut:
                return TweenFunctions.QuintEaseOut(t, from, to, 1f);
            case EaseType.QuintEaseIn:
                return TweenFunctions.QuintEaseIn(t, from, to, 1f);
            case EaseType.QuintEaseInOut:
                return TweenFunctions.QuintEaseInOut(t, from, to, 1f);
            case EaseType.QuintEaseOutIn:
                return TweenFunctions.QuintEaseOutIn(t, from, to, 1f);
            case EaseType.CircEaseOut:
                return TweenFunctions.CircEaseOut(t, from, to, 1f);
            case EaseType.CircEaseIn:
                return TweenFunctions.CircEaseIn(t, from, to, 1f);
            case EaseType.CircEaseInOut:
                return TweenFunctions.CircEaseInOut(t, from, to, 1f);
            case EaseType.CircEaseOutIn:
                return TweenFunctions.CircEaseOutIn(t, from, to, 1f);
            case EaseType.SineEaseOut:
                return TweenFunctions.SineEaseOut(t, from, to, 1f);
            case EaseType.SineEaseIn:
                return TweenFunctions.SineEaseIn(t, from, to, 1f);
            case EaseType.SineEaseInOut:
                return TweenFunctions.SineEaseInOut(t, from, to, 1f);
            case EaseType.SineEaseOutIn:
                return TweenFunctions.SineEaseOutIn(t, from, to, 1f);
            case EaseType.ElasticEaseOut:
                return TweenFunctions.ElasticEaseOut(t, from, to, 1f);
            case EaseType.ElasticEaseIn:
                return TweenFunctions.ElasticEaseIn(t, from, to, 1f);
            case EaseType.ElasticEaseInOut:
                return TweenFunctions.ElasticEaseInOut(t, from, to, 1f);
            case EaseType.ElasticEaseOutIn:
                return TweenFunctions.ElasticEaseOutIn(t, from, to, 1f);
            case EaseType.BounceEaseOut:
                return TweenFunctions.BounceEaseOut(t, from, to, 1f);
            case EaseType.BounceEaseIn:
                return TweenFunctions.BounceEaseIn(t, from, to, 1f);
            case EaseType.BounceEaseInOut:
                return TweenFunctions.BounceEaseInOut(t, from, to, 1f);
            case EaseType.BounceEaseOutIn:
                return TweenFunctions.BounceEaseOutIn(t, from, to, 1f);
            case EaseType.BackEaseOut:
                return TweenFunctions.BackEaseOut(t, from, to, 1f);
            case EaseType.BackEaseIn:
                return TweenFunctions.BackEaseIn(t, from, to, 1f);
            case EaseType.BackEaseInOut:
                return TweenFunctions.BackEaseInOut(t, from, to, 1f);
            case EaseType.BackEaseOutIn:
                return TweenFunctions.BackEaseOutIn(t, from, to, 1f);
            default:
                throw new ArgumentOutOfRangeException("type");
        }
    }

    /// <summary>
    /// Calculate a framerate-independent value to lerp by
    /// </summary>
    /// <param name="strength"></param>
    /// <param name="deltaTime"></param>
    /// <returns></returns>
    public static float SpringLerp(float strength, float deltaTime)
    {
        var ms = Mathf.RoundToInt(deltaTime*1000f);
        var step = 0.001f*strength;

        var from = 0f;
        var to = 1f;

        for (var i = 0; i < ms; i++)
        {
            from = Mathf.Lerp(from, to, step);
        }

        return from;
    }

    /// <summary>
    /// A frame-rate independent way of doing Mathf.Lerp(from, to, Time.deltaTime * strength). Based on NGUIMath.SpringLerp
    /// </summary>
    /// <param name="from">Starting Value</param>
    /// <param name="to">End Value</param>
    /// <param name="strength">How fast the spring will complete</param>
    /// <param name="deltaTime">Pass in Time.deltaTime or RealTime.deltaTime</param>
    /// <returns>Interpolated value</returns>
    public static float SpringLerp(float from, float to, float strength, float deltaTime)
    {
        return Mathf.Lerp(from, to, SpringLerp(strength, deltaTime));
    }

    public static Vector3 SpringLerp(Vector3 from, Vector3 to, float strength, float deltaTime)
    {
        return Vector3.Lerp(from, to, SpringLerp(strength, deltaTime));
    }

    public static Quaternion SpringLerp(Quaternion from, Quaternion to, float strength, float deltaTime)
    {
        return Quaternion.Slerp(from, to, SpringLerp(strength, deltaTime));
    }

    /// <summary>
    /// Smoothly clamp value between 0 and max, smoothing between min and max
    /// </summary>
    /// <param name="value"></param>
    /// <param name="min"></param>
    /// <param name="max"></param>
    /// <param name="easeType"></param>
    /// <returns></returns>
    public static float SmoothClamp(float value, float min, float max, float scrollMax,
        EaseType easeType = EaseType.ExpoEaseOut)
    {
        if (value < min)
        {
            return value;
        }

        var p = Mathf.Clamp01((value - min)/(scrollMax - min));

        Debug.Log(p);

        return Mathf.Clamp(min + Mathf.Lerp(value - min, max, Ease(0, 1f, p, easeType)), 0, max);
    }
}
