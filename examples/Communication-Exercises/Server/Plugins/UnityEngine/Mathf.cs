using System;

namespace UnityEngine
{
    public static class Mathf
    {
        public const float Epsilon = 0.00001F;

        public const float CosAngle20 = 0.9396926208f;
        public const float CompareEpsilon = 0.000001f;

        /// <summary>
        ///     <para>Returns the sine of angle f.</para>
        /// </summary>
        /// <param name="f">The input angle, in radians.</param>
        /// <returns>
        ///     <para>The return value between -1 and +1.</para>
        /// </returns>
        public static float Sin(float f)
        {
            return (float) Math.Sin(f);
        }

        /// <summary>
        ///     <para>Returns the cosine of angle f.</para>
        /// </summary>
        /// <param name="f">The input angle, in radians.</param>
        /// <returns>
        ///     <para>The return value between -1 and 1.</para>
        /// </returns>
        public static float Cos(float f)
        {
            return (float) Math.Cos(f);
        }

        /// <summary>
        ///     <para>Returns the tangent of angle f in radians.</para>
        /// </summary>
        /// <param name="f"></param>
        public static float Tan(float f)
        {
            return (float) Math.Tan(f);
        }

        /// <summary>
        ///     <para>Returns the arc-sine of f - the angle in radians whose sine is f.</para>
        /// </summary>
        /// <param name="f"></param>
        public static float Asin(float f)
        {
            return (float) Math.Asin(f);
        }

        /// <summary>
        ///     <para>Returns the arc-cosine of f - the angle in radians whose cosine is f.</para>
        /// </summary>
        /// <param name="f"></param>
        public static float Acos(float f)
        {
            return (float) Math.Acos(f);
        }

        /// <summary>
        ///     <para>Returns the arc-tangent of f - the angle in radians whose tangent is f.</para>
        /// </summary>
        /// <param name="f"></param>
        public static float Atan(float f)
        {
            return (float) Math.Atan(f);
        }

        /// <summary>
        ///     <para>Returns the angle in radians whose Tan is y/x.</para>
        /// </summary>
        /// <param name="y"></param>
        /// <param name="x"></param>
        public static float Atan2(float y, float x)
        {
            return (float) Math.Atan2(y, x);
        }

        /// <summary>
        ///     <para>Returns square root of f.</para>
        /// </summary>
        /// <param name="f"></param>
        public static float Sqrt(float f)
        {
            return (float) Math.Sqrt(f);
        }

        /// <summary>
        ///     <para>Returns the absolute value of f.</para>
        /// </summary>
        /// <param name="f"></param>
        public static float Abs(float f)
        {
            return Math.Abs(f);
        }

        /// <summary>
        ///     <para>Returns the absolute value of value.</para>
        /// </summary>
        /// <param name="value"></param>
        public static int Abs(int value)
        {
            return Math.Abs(value);
        }

        /// <summary>
        ///     <para>Returns the smallest of two or more values.</para>
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="values"></param>
        public static float Min(float a, float b)
        {
            return a >= (double) b ? b : a;
        }

        /// <summary>
        ///     <para>Returns the smallest of two or more values.</para>
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="values"></param>
        public static float Min(params float[] values)
        {
            var length = values.Length;
            if (length == 0)
            {
                return 0.0f;
            }

            var num = values[0];
            for (var index = 1 ; index < length ; ++index)
            {
                if (values[index] < (double) num)
                {
                    num = values[index];
                }
            }

            return num;
        }

        /// <summary>
        ///     <para>Returns the smallest of two or more values.</para>
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="values"></param>
        public static int Min(int a, int b)
        {
            return a >= b ? b : a;
        }

        /// <summary>
        ///     <para>Returns the smallest of two or more values.</para>
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="values"></param>
        public static int Min(params int[] values)
        {
            var length = values.Length;
            if (length == 0)
            {
                return 0;
            }

            var num = values[0];
            for (var index = 1 ; index < length ; ++index)
            {
                if (values[index] < num)
                {
                    num = values[index];
                }
            }

            return num;
        }

        /// <summary>
        ///     <para>Returns largest of two or more values.</para>
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="values"></param>
        public static float Max(float a, float b)
        {
            return a <= (double) b ? b : a;
        }

        /// <summary>
        ///     <para>Returns largest of two or more values.</para>
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="values"></param>
        public static float Max(params float[] values)
        {
            var length = values.Length;
            if (length == 0)
            {
                return 0.0f;
            }

            var num = values[0];
            for (var index = 1 ; index < length ; ++index)
            {
                if (values[index] > (double) num)
                {
                    num = values[index];
                }
            }

            return num;
        }

        /// <summary>
        ///     <para>Returns the largest of two or more values.</para>
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="values"></param>
        public static int Max(int a, int b)
        {
            return a <= b ? b : a;
        }

        /// <summary>
        ///     <para>Returns the largest of two or more values.</para>
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="values"></param>
        public static int Max(params int[] values)
        {
            var length = values.Length;
            if (length == 0)
            {
                return 0;
            }

            var num = values[0];
            for (var index = 1 ; index < length ; ++index)
            {
                if (values[index] > num)
                {
                    num = values[index];
                }
            }

            return num;
        }

        /// <summary>
        ///     <para>Returns f raised to power p.</para>
        /// </summary>
        /// <param name="f"></param>
        /// <param name="p"></param>
        public static float Pow(float f, float p)
        {
            return (float) Math.Pow(f, p);
        }

        /// <summary>
        ///     <para>Returns e raised to the specified power.</para>
        /// </summary>
        /// <param name="power"></param>
        public static float Exp(float power)
        {
            return (float) Math.Exp(power);
        }

        /// <summary>
        ///     <para>Returns the logarithm of a specified number in a specified base.</para>
        /// </summary>
        /// <param name="f"></param>
        /// <param name="p"></param>
        public static float Log(float f, float p)
        {
            return (float) Math.Log(f, p);
        }

        /// <summary>
        ///     <para>Returns the natural (base e) logarithm of a specified number.</para>
        /// </summary>
        /// <param name="f"></param>
        public static float Log(float f)
        {
            return (float) Math.Log(f);
        }

        /// <summary>
        ///     <para>Returns the base 10 logarithm of a specified number.</para>
        /// </summary>
        /// <param name="f"></param>
        public static float Log10(float f)
        {
            return (float) Math.Log10(f);
        }

        /// <summary>
        ///     <para>Returns the smallest integer greater to or equal to f.</para>
        /// </summary>
        /// <param name="f"></param>
        public static float Ceil(float f)
        {
            return (float) Math.Ceiling(f);
        }

        /// <summary>
        ///     <para>Returns the largest integer smaller to or equal to f.</para>
        /// </summary>
        /// <param name="f"></param>
        public static float Floor(float f)
        {
            return (float) Math.Floor(f);
        }

        /// <summary>
        ///     <para>Returns f rounded to the nearest integer.</para>
        /// </summary>
        /// <param name="f"></param>
        public static float Round(float f)
        {
            return (float) Math.Round(f);
        }

        /// <summary>
        ///     <para>Returns the smallest integer greater to or equal to f.</para>
        /// </summary>
        /// <param name="f"></param>
        public static int CeilToInt(float f)
        {
            return (int) Math.Ceiling(f);
        }

        /// <summary>
        ///     <para>Returns the largest integer smaller to or equal to f.</para>
        /// </summary>
        /// <param name="f"></param>
        public static int FloorToInt(float f)
        {
            return (int) Math.Floor(f);
        }

        /// <summary>
        ///     <para>Returns f rounded to the nearest integer.</para>
        /// </summary>
        /// <param name="f"></param>
        public static int RoundToInt(float f)
        {
            return (int) Math.Round(f);
        }

        /// <summary>
        ///     <para>Returns the sign of f.</para>
        /// </summary>
        /// <param name="f"></param>
        public static float Sign(float f)
        {
            return f < 0.0 ? -1f : 1f;
        }

        /// <summary>
        ///     <para>Clamps a value between a minimum float and maximum float value.</para>
        /// </summary>
        /// <param name="value"></param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        public static float Clamp(float value, float min, float max)
        {
            if (value < (double) min)
            {
                value = min;
            }
            else if (value > (double) max)
            {
                value = max;
            }

            return value;
        }

        /// <summary>
        ///     <para>Clamps value between min and max and returns value.</para>
        /// </summary>
        /// <param name="value"></param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        public static int Clamp(int value, int min, int max)
        {
            if (value < min)
            {
                value = min;
            }
            else if (value > max)
            {
                value = max;
            }

            return value;
        }

        /// <summary>
        ///     <para>Clamps value between 0 and 1 and returns value.</para>
        /// </summary>
        /// <param name="value"></param>
        public static float Clamp01(float value)
        {
            if (value < 0.0)
            {
                return 0.0f;
            }

            if (value > 1.0)
            {
                return 1f;
            }

            return value;
        }

        /// <summary>
        ///     <para>Linearly interpolates between a and b by t.</para>
        /// </summary>
        /// <param name="a">The start value.</param>
        /// <param name="b">The end value.</param>
        /// <param name="t">The interpolation value between the two floats.</param>
        /// <returns>
        ///     <para>The interpolated float result between the two float values.</para>
        /// </returns>
        public static float Lerp(float a, float b, float t)
        {
            return a + (b - a) * Clamp01(t);
        }

        /// <summary>
        ///     <para>Linearly interpolates between a and b by t with no limit to t.</para>
        /// </summary>
        /// <param name="a">The start value.</param>
        /// <param name="b">The end value.</param>
        /// <param name="t">The interpolation between the two floats.</param>
        /// <returns>
        ///     <para>The float value as a result from the linear interpolation.</para>
        /// </returns>
        public static float LerpUnclamped(float a, float b, float t)
        {
            return a + (b - a) * t;
        }

        /// <summary>
        ///     <para>Same as Lerp but makes sure the values interpolate correctly when they wrap around 360 degrees.</para>
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="t"></param>
        public static float LerpAngle(float a, float b, float t)
        {
            var num = Repeat(b - a, 360f);
            if (num > 180.0)
            {
                num -= 360f;
            }

            return a + num * Clamp01(t);
        }

        /// <summary>
        ///     <para>Moves a value current towards target.</para>
        /// </summary>
        /// <param name="current">The current value.</param>
        /// <param name="target">The value to move towards.</param>
        /// <param name="maxDelta">The maximum change that should be applied to the value.</param>
        public static float MoveTowards(float current, float target, float maxDelta)
        {
            if (Abs(target - current) <= (double) maxDelta)
            {
                return target;
            }

            return current + Sign(target - current) * maxDelta;
        }

        /// <summary>
        ///     <para>Same as MoveTowards but makes sure the values interpolate correctly when they wrap around 360 degrees.</para>
        /// </summary>
        /// <param name="current"></param>
        /// <param name="target"></param>
        /// <param name="maxDelta"></param>
        public static float MoveTowardsAngle(float current, float target, float maxDelta)
        {
            var num = DeltaAngle(current, target);
            if (-(double) maxDelta < num && num < (double) maxDelta)
            {
                return target;
            }

            target = current + num;
            return MoveTowards(current, target, maxDelta);
        }

        /// <summary>
        ///     <para>Interpolates between min and max with smoothing at the limits.</para>
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <param name="t"></param>
        public static float SmoothStep(float from, float to, float t)
        {
            t = Clamp01(t);
            t = (float) (-2.0 * t * t * t + 3.0 * t * t);
            return (float) (to * (double) t + from * (1.0 - t));
        }

        public static float Gamma(float value, float absmax, float gamma)
        {
            var flag = false;
            if (value < 0.0)
            {
                flag = true;
            }

            var num1 = Abs(value);
            if (num1 > (double) absmax)
            {
                return !flag ? num1 : -num1;
            }

            var num2 = Pow(num1 / absmax, gamma) * absmax;
            return !flag ? num2 : -num2;
        }

        /// <summary>
        ///     <para>Loops the value t, so that it is never larger than length and never smaller than 0.</para>
        /// </summary>
        /// <param name="t"></param>
        /// <param name="length"></param>
        public static float Repeat(float t, float length)
        {
            return Clamp(t - Floor(t / length) * length, 0.0f, length);
        }

        /// <summary>
        ///     <para>PingPongs the value t, so that it is never larger than length and never smaller than 0.</para>
        /// </summary>
        /// <param name="t"></param>
        /// <param name="length"></param>
        public static float PingPong(float t, float length)
        {
            t = Repeat(t, length * 2f);
            return length - Abs(t - length);
        }

        /// <summary>
        ///     <para>Calculates the linear parameter t that produces the interpolant value within the range [a, b].</para>
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="value"></param>
        public static float InverseLerp(float a, float b, float value)
        {
            if (a != (double) b)
            {
                return Clamp01((float) ((value - (double) a) / (b - (double) a)));
            }

            return 0.0f;
        }

        /// <summary>
        ///     <para>Calculates the shortest difference between two given angles given in degrees.</para>
        /// </summary>
        /// <param name="current"></param>
        /// <param name="target"></param>
        public static float DeltaAngle(float current, float target)
        {
            var num = Repeat(target - current, 360f);
            if (num > 180.0)
            {
                num -= 360f;
            }

            return num;
        }

        internal static bool LineIntersection(Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4, ref Vector2 result)
        {
            var num1 = p2.x - p1.x;
            var num2 = p2.y - p1.y;
            var num3 = p4.x - p3.x;
            var num4 = p4.y - p3.y;
            var num5 = (float) (num1 * (double) num4 - num2 * (double) num3);
            if (num5 == 0.0)
            {
                return false;
            }

            var num6 = p3.x - p1.x;
            var num7 = p3.y - p1.y;
            var num8 = (float) (num6 * (double) num4 - num7 * (double) num3) / num5;
            result = new Vector2(p1.x + num8 * num1, p1.y + num8 * num2);
            return true;
        }

        internal static bool LineSegmentIntersection(Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4, ref Vector2 result)
        {
            var num1 = p2.x - p1.x;
            var num2 = p2.y - p1.y;
            var num3 = p4.x - p3.x;
            var num4 = p4.y - p3.y;
            var num5 = (float) (num1 * (double) num4 - num2 * (double) num3);
            if (num5 == 0.0)
            {
                return false;
            }

            var num6 = p3.x - p1.x;
            var num7 = p3.y - p1.y;
            var num8 = (float) (num6 * (double) num4 - num7 * (double) num3) / num5;
            if (num8 < 0.0 || num8 > 1.0)
            {
                return false;
            }

            var num9 = (float) (num6 * (double) num2 - num7 * (double) num1) / num5;
            if (num9 < 0.0 || num9 > 1.0)
            {
                return false;
            }

            result = new Vector2(p1.x + num8 * num1, p1.y + num8 * num2);
            return true;
        }

        internal static long RandomToLong(Random r)
        {
            var buffer = new byte[8];
            r.NextBytes(buffer);
            return (long) BitConverter.ToUInt64(buffer, 0) & long.MaxValue;
        }

        public static float Rad2Deg(float radians)
        {
            return (float) (radians * 180 / Math.PI);
        }

        public static float Deg2Rad(float degrees)
        {
            return (float) (degrees * Math.PI / 180);
        }

        public static Vector3 Rad2Deg(Vector3 radians)
        {
            return new Vector3((float) (radians.x * 180 / Math.PI), (float) (radians.y * 180 / Math.PI),
                (float) (radians.z * 180 / Math.PI));
        }

        public static Vector3 Deg2Rad(Vector3 degrees)
        {
            return new Vector3((float) (degrees.x * Math.PI / 180), (float) (degrees.y * Math.PI / 180),
                (float) (degrees.z * Math.PI / 180));
        }

        public static bool CompareApproximate(float f0, float f1, float epsilon = CompareEpsilon)
        {
            return Math.Abs(f0 - f1) < epsilon;
        }

        public static bool CompareApproximate(double f0, double f1, float epsilon = CompareEpsilon)
        {
            return Math.Abs(f0 - f1) < epsilon;
        }
    }
}