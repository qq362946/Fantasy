using System;
using System.Globalization;

namespace UnityEngine
{
    [Serializable]
    public struct Vector3 : IEquatable<Vector3>
    {
        private const float k1OverSqrt2 = 0.7071068f;
        private const float epsilon = 1E-05f;
        public static readonly Vector3 zero = new();
        public static readonly Vector3 one = new(1f, 1f, 1f);
        public static readonly Vector3 up = new(0.0f, 1f, 0.0f);
        public static readonly Vector3 down = new(0.0f, -1f, 0.0f);
        public static readonly Vector3 right = new(1f, 0.0f, 0.0f);
        public static readonly Vector3 left = new(-1f, 0.0f, 0.0f);
        public static readonly Vector3 forward = new(0.0f, 0.0f, 1f);
        public static readonly Vector3 back = new(0.0f, 0.0f, -1f);
        public float x;
        public float y;
        public float z;

        public Vector3(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public Vector3(float value)
        {
            x = y = z = value;
        }

        public Vector3(Vector2 value, float z)
        {
            x = value.x;
            y = value.y;
            this.z = z;
        }

        public float magnitude => Length();

        public float sqrMagnitude => LengthSquared();

        public Vector3 normalized => Normalize(this);

        public bool Equals(Vector3 other)
        {
            if (x == (double) other.x && y == (double) other.y)
            {
                return z == (double) other.z;
            }

            return false;
        }

        public override string ToString()
        {
            var currentCulture = CultureInfo.CurrentCulture;
            return string.Format(currentCulture, "({0}, {1}, {2})", x.ToString(currentCulture),
                y.ToString(currentCulture), z.ToString(currentCulture));
        }

        public override bool Equals(object obj)
        {
            var flag = false;
            if (obj is Vector3)
            {
                flag = Equals((Vector3) obj);
            }

            return flag;
        }

        public override int GetHashCode()
        {
            return x.GetHashCode() + y.GetHashCode() + z.GetHashCode();
        }

        public float Length()
        {
            return (float) Math.Sqrt(x * (double) x + y * (double) y + z * (double) z);
        }

        public float LengthSquared()
        {
            return (float) (x * (double) x + y * (double) y + z * (double) z);
        }

        public static float Distance(Vector3 value1, Vector3 value2)
        {
            var num1 = value1.x - value2.x;
            var num2 = value1.y - value2.y;
            var num3 = value1.z - value2.z;
            return (float) Math.Sqrt(num1 * (double) num1 + num2 * (double) num2 + num3 * (double) num3);
        }

        public static float DistanceNotSqrt(Vector3 a, Vector3 b)
        {
            return (float) Math.Pow((a.x - b.x) * (a.x - b.x) + (a.y - b.y) * (a.y - b.y) + (a.z - b.y) * (a.y - b.y), 0.5);
        }

        public static void Distance(ref Vector3 value1, ref Vector3 value2, out float result)
        {
            var num1 = value1.x - value2.x;
            var num2 = value1.y - value2.y;
            var num3 = value1.z - value2.z;
            var num4 = (float) (num1 * (double) num1 + num2 * (double) num2 + num3 * (double) num3);
            result = (float) Math.Sqrt(num4);
        }

        public static float DistanceSquared(Vector3 value1, Vector3 value2)
        {
            var num1 = value1.x - value2.x;
            var num2 = value1.y - value2.y;
            var num3 = value1.z - value2.z;
            return (float) (num1 * (double) num1 + num2 * (double) num2 + num3 * (double) num3);
        }

        public static void DistanceSquared(ref Vector3 value1, ref Vector3 value2, out float result)
        {
            var num1 = value1.x - value2.x;
            var num2 = value1.y - value2.y;
            var num3 = value1.z - value2.z;
            result = (float) (num1 * (double) num1 + num2 * (double) num2 + num3 * (double) num3);
        }
        
        public static float SqrDistance(Vector3 a, Vector3 b)
        {
            var sqrX = (a.x - b.x) * (a.x - b.x);
            var sqrY = (a.y - b.y) * (a.x - b.x);
            var sqrZ = (a.z - b.z) * (a.z - b.z);
            return sqrX + sqrY + sqrZ;
        }

        public static bool NotAtDistance(Vector3 a, Vector3 b, float minDistance)
        {
            return SqrDistance(a, b) >= minDistance * minDistance;
        }
        
        public static bool NotAtDistance(float sqrDistance, float minDistance)
        {
            return sqrDistance >= minDistance * minDistance;
        }

        public static float Dot(Vector3 vector1, Vector3 vector2)
        {
            return (float) (vector1.x * (double) vector2.x + vector1.y * (double) vector2.y +
                            vector1.z * (double) vector2.z);
        }

        public static void Dot(ref Vector3 vector1, ref Vector3 vector2, out float result)
        {
            result = (float) (vector1.x * (double) vector2.x + vector1.y * (double) vector2.y +
                              vector1.z * (double) vector2.z);
        }

        public void Normalize()
        {
            var num1 = (float) (x * (double) x + y * (double) y + z * (double) z);
            if (num1 < (double) Mathf.Epsilon)
            {
                return;
            }

            var num2 = 1f / (float) Math.Sqrt(num1);
            x *= num2;
            y *= num2;
            z *= num2;
        }

        public static Vector3 Normalize(Vector3 value)
        {
            var num1 = (float) (value.x * (double) value.x + value.y * (double) value.y + value.z * (double) value.z);
            if (num1 < (double) Mathf.Epsilon)
            {
                return value;
            }

            var num2 = 1f / (float) Math.Sqrt(num1);
            Vector3 vector3;
            vector3.x = value.x * num2;
            vector3.y = value.y * num2;
            vector3.z = value.z * num2;
            return vector3;
        }

        public static void Normalize(ref Vector3 value, out Vector3 result)
        {
            var num1 = (float) (value.x * (double) value.x + value.y * (double) value.y + value.z * (double) value.z);
            if (num1 < (double) Mathf.Epsilon)
            {
                result = value;
            }
            else
            {
                var num2 = 1f / (float) Math.Sqrt(num1);
                result.x = value.x * num2;
                result.y = value.y * num2;
                result.z = value.z * num2;
            }
        }

        public static Vector3 Cross(Vector3 vector1, Vector3 vector2)
        {
            Vector3 vector3;
            vector3.x = (float) (vector1.y * (double) vector2.z - vector1.z * (double) vector2.y);
            vector3.y = (float) (vector1.z * (double) vector2.x - vector1.x * (double) vector2.z);
            vector3.z = (float) (vector1.x * (double) vector2.y - vector1.y * (double) vector2.x);
            return vector3;
        }

        public static void Cross(ref Vector3 vector1, ref Vector3 vector2, out Vector3 result)
        {
            var num1 = (float) (vector1.y * (double) vector2.z - vector1.z * (double) vector2.y);
            var num2 = (float) (vector1.z * (double) vector2.x - vector1.x * (double) vector2.z);
            var num3 = (float) (vector1.x * (double) vector2.y - vector1.y * (double) vector2.x);
            result.x = num1;
            result.y = num2;
            result.z = num3;
        }

        public static Vector3 Reflect(Vector3 vector, Vector3 normal)
        {
            var num = (float) (vector.x * (double) normal.x + vector.y * (double) normal.y +
                               vector.z * (double) normal.z);
            Vector3 vector3;
            vector3.x = vector.x - 2f * num * normal.x;
            vector3.y = vector.y - 2f * num * normal.y;
            vector3.z = vector.z - 2f * num * normal.z;
            return vector3;
        }

        public static void Reflect(ref Vector3 vector, ref Vector3 normal, out Vector3 result)
        {
            var num = (float) (vector.x * (double) normal.x + vector.y * (double) normal.y +
                               vector.z * (double) normal.z);
            result.x = vector.x - 2f * num * normal.x;
            result.y = vector.y - 2f * num * normal.y;
            result.z = vector.z - 2f * num * normal.z;
        }

        public static Vector3 Min(Vector3 value1, Vector3 value2)
        {
            Vector3 vector3;
            vector3.x = value1.x < (double) value2.x ? value1.x : value2.x;
            vector3.y = value1.y < (double) value2.y ? value1.y : value2.y;
            vector3.z = value1.z < (double) value2.z ? value1.z : value2.z;
            return vector3;
        }

        public static void Min(ref Vector3 value1, ref Vector3 value2, out Vector3 result)
        {
            result.x = value1.x < (double) value2.x ? value1.x : value2.x;
            result.y = value1.y < (double) value2.y ? value1.y : value2.y;
            result.z = value1.z < (double) value2.z ? value1.z : value2.z;
        }

        public static Vector3 Max(Vector3 value1, Vector3 value2)
        {
            Vector3 vector3;
            vector3.x = value1.x > (double) value2.x ? value1.x : value2.x;
            vector3.y = value1.y > (double) value2.y ? value1.y : value2.y;
            vector3.z = value1.z > (double) value2.z ? value1.z : value2.z;
            return vector3;
        }

        public static void Max(ref Vector3 value1, ref Vector3 value2, out Vector3 result)
        {
            result.x = value1.x > (double) value2.x ? value1.x : value2.x;
            result.y = value1.y > (double) value2.y ? value1.y : value2.y;
            result.z = value1.z > (double) value2.z ? value1.z : value2.z;
        }

        public static Vector3 Clamp(Vector3 value1, Vector3 min, Vector3 max)
        {
            var x = value1.x;
            var num1 = x > (double) max.x ? max.x : x;
            var num2 = num1 < (double) min.x ? min.x : num1;
            var y = value1.y;
            var num3 = y > (double) max.y ? max.y : y;
            var num4 = num3 < (double) min.y ? min.y : num3;
            var z = value1.z;
            var num5 = z > (double) max.z ? max.z : z;
            var num6 = num5 < (double) min.z ? min.z : num5;
            Vector3 vector3;
            vector3.x = num2;
            vector3.y = num4;
            vector3.z = num6;
            return vector3;
        }

        public static void Clamp(ref Vector3 value1, ref Vector3 min, ref Vector3 max, out Vector3 result)
        {
            var x = value1.x;
            var num1 = x > (double) max.x ? max.x : x;
            var num2 = num1 < (double) min.x ? min.x : num1;
            var y = value1.y;
            var num3 = y > (double) max.y ? max.y : y;
            var num4 = num3 < (double) min.y ? min.y : num3;
            var z = value1.z;
            var num5 = z > (double) max.z ? max.z : z;
            var num6 = num5 < (double) min.z ? min.z : num5;
            result.x = num2;
            result.y = num4;
            result.z = num6;
        }

        public static Vector3 Lerp(Vector3 value1, Vector3 value2, float amount)
        {
            Vector3 vector3;
            vector3.x = value1.x + (value2.x - value1.x) * amount;
            vector3.y = value1.y + (value2.y - value1.y) * amount;
            vector3.z = value1.z + (value2.z - value1.z) * amount;
            return vector3;
        }

        public static void Lerp(ref Vector3 value1, ref Vector3 value2, float amount, out Vector3 result)
        {
            result.x = value1.x + (value2.x - value1.x) * amount;
            result.y = value1.y + (value2.y - value1.y) * amount;
            result.z = value1.z + (value2.z - value1.z) * amount;
        }

        public static Vector3 SmoothStep(Vector3 value1, Vector3 value2, float amount)
        {
            amount = amount > 1.0 ? 1f : amount < 0.0 ? 0.0f : amount;
            amount = (float) (amount * (double) amount * (3.0 - 2.0 * amount));
            Vector3 vector3;
            vector3.x = value1.x + (value2.x - value1.x) * amount;
            vector3.y = value1.y + (value2.y - value1.y) * amount;
            vector3.z = value1.z + (value2.z - value1.z) * amount;
            return vector3;
        }

        public static void SmoothStep(ref Vector3 value1, ref Vector3 value2, float amount, out Vector3 result)
        {
            amount = amount > 1.0 ? 1f : amount < 0.0 ? 0.0f : amount;
            amount = (float) (amount * (double) amount * (3.0 - 2.0 * amount));
            result.x = value1.x + (value2.x - value1.x) * amount;
            result.y = value1.y + (value2.y - value1.y) * amount;
            result.z = value1.z + (value2.z - value1.z) * amount;
        }

        public static Vector3 Hermite(Vector3 value1, Vector3 tangent1, Vector3 value2, Vector3 tangent2, float amount)
        {
            var num1 = amount * amount;
            var num2 = amount * num1;
            var num3 = (float) (2.0 * num2 - 3.0 * num1 + 1.0);
            var num4 = (float) (-2.0 * num2 + 3.0 * num1);
            var num5 = num2 - 2f * num1 + amount;
            var num6 = num2 - num1;
            Vector3 vector3;
            vector3.x = (float) (value1.x * (double) num3 + value2.x * (double) num4 + tangent1.x * (double) num5 +
                                 tangent2.x * (double) num6);
            vector3.y = (float) (value1.y * (double) num3 + value2.y * (double) num4 + tangent1.y * (double) num5 +
                                 tangent2.y * (double) num6);
            vector3.z = (float) (value1.z * (double) num3 + value2.z * (double) num4 + tangent1.z * (double) num5 +
                                 tangent2.z * (double) num6);
            return vector3;
        }

        public static void Hermite(ref Vector3 value1, ref Vector3 tangent1, ref Vector3 value2, ref Vector3 tangent2,
            float amount, out Vector3 result)
        {
            var num1 = amount * amount;
            var num2 = amount * num1;
            var num3 = (float) (2.0 * num2 - 3.0 * num1 + 1.0);
            var num4 = (float) (-2.0 * num2 + 3.0 * num1);
            var num5 = num2 - 2f * num1 + amount;
            var num6 = num2 - num1;
            result.x = (float) (value1.x * (double) num3 + value2.x * (double) num4 + tangent1.x * (double) num5 +
                                tangent2.x * (double) num6);
            result.y = (float) (value1.y * (double) num3 + value2.y * (double) num4 + tangent1.y * (double) num5 +
                                tangent2.y * (double) num6);
            result.z = (float) (value1.z * (double) num3 + value2.z * (double) num4 + tangent1.z * (double) num5 +
                                tangent2.z * (double) num6);
        }

        public static Vector3 Negate(Vector3 value)
        {
            Vector3 vector3;
            vector3.x = -value.x;
            vector3.y = -value.y;
            vector3.z = -value.z;
            return vector3;
        }

        public static void Negate(ref Vector3 value, out Vector3 result)
        {
            result.x = -value.x;
            result.y = -value.y;
            result.z = -value.z;
        }

        public static Vector3 Add(Vector3 value1, Vector3 value2)
        {
            Vector3 vector3;
            vector3.x = value1.x + value2.x;
            vector3.y = value1.y + value2.y;
            vector3.z = value1.z + value2.z;
            return vector3;
        }

        public static void Add(ref Vector3 value1, ref Vector3 value2, out Vector3 result)
        {
            result.x = value1.x + value2.x;
            result.y = value1.y + value2.y;
            result.z = value1.z + value2.z;
        }

        public static Vector3 Sub(Vector3 value1, Vector3 value2)
        {
            Vector3 vector3;
            vector3.x = value1.x - value2.x;
            vector3.y = value1.y - value2.y;
            vector3.z = value1.z - value2.z;
            return vector3;
        }

        public static void Sub(ref Vector3 value1, ref Vector3 value2, out Vector3 result)
        {
            result.x = value1.x - value2.x;
            result.y = value1.y - value2.y;
            result.z = value1.z - value2.z;
        }

        public static Vector3 Multiply(Vector3 value1, Vector3 value2)
        {
            Vector3 vector3;
            vector3.x = value1.x * value2.x;
            vector3.y = value1.y * value2.y;
            vector3.z = value1.z * value2.z;
            return vector3;
        }

        public static void Multiply(ref Vector3 value1, ref Vector3 value2, out Vector3 result)
        {
            result.x = value1.x * value2.x;
            result.y = value1.y * value2.y;
            result.z = value1.z * value2.z;
        }

        public static Vector3 Multiply(Vector3 value1, float scaleFactor)
        {
            Vector3 vector3;
            vector3.x = value1.x * scaleFactor;
            vector3.y = value1.y * scaleFactor;
            vector3.z = value1.z * scaleFactor;
            return vector3;
        }

        public static void Multiply(ref Vector3 value1, float scaleFactor, out Vector3 result)
        {
            result.x = value1.x * scaleFactor;
            result.y = value1.y * scaleFactor;
            result.z = value1.z * scaleFactor;
        }

        public static Vector3 Divide(Vector3 value1, Vector3 value2)
        {
            Vector3 vector3;
            vector3.x = value1.x / value2.x;
            vector3.y = value1.y / value2.y;
            vector3.z = value1.z / value2.z;
            return vector3;
        }

        public static void Divide(ref Vector3 value1, ref Vector3 value2, out Vector3 result)
        {
            result.x = value1.x / value2.x;
            result.y = value1.y / value2.y;
            result.z = value1.z / value2.z;
        }

        public static Vector3 Divide(Vector3 value1, float divider)
        {
            var num = 1f / divider;
            Vector3 vector3;
            vector3.x = value1.x * num;
            vector3.y = value1.y * num;
            vector3.z = value1.z * num;
            return vector3;
        }

        public static void Divide(ref Vector3 value1, float divider, out Vector3 result)
        {
            var num = 1f / divider;
            result.x = value1.x * num;
            result.y = value1.y * num;
            result.z = value1.z * num;
        }

        private static float magnitudeStatic(ref Vector3 inV)
        {
            return (float) Math.Sqrt(Dot(inV, inV));
        }

        private static Vector3 orthoNormalVectorFast(ref Vector3 n)
        {
            Vector3 vector3;
            if (Math.Abs(n.z) > (double) k1OverSqrt2)
            {
                var num = 1f / (float) Math.Sqrt(n.y * (double) n.y + n.z * (double) n.z);
                vector3.x = 0.0f;
                vector3.y = -n.z * num;
                vector3.z = n.y * num;
            }
            else
            {
                var num = 1f / (float) Math.Sqrt(n.x * (double) n.x + n.y * (double) n.y);
                vector3.x = -n.y * num;
                vector3.y = n.x * num;
                vector3.z = 0.0f;
            }

            return vector3;
        }

        public static void OrthoNormalize(ref Vector3 normal, ref Vector3 tangent)
        {
            var num1 = magnitudeStatic(ref normal);
            if (num1 > (double) Mathf.Epsilon)
            {
                normal /= num1;
            }
            else
            {
                normal = new Vector3(1f, 0.0f, 0.0f);
            }

            var num2 = Dot(normal, tangent);
            tangent -= num2 * normal;
            var num3 = magnitudeStatic(ref tangent);
            if (num3 < (double) Mathf.Epsilon)
            {
                tangent = orthoNormalVectorFast(ref normal);
            }
            else
            {
                tangent /= num3;
            }
        }

        public static void OrthoNormalize(ref Vector3 normal, ref Vector3 tangent, ref Vector3 binormal)
        {
            var num1 = magnitudeStatic(ref normal);
            if (num1 > (double) Mathf.Epsilon)
            {
                normal /= num1;
            }
            else
            {
                normal = new Vector3(1f, 0.0f, 0.0f);
            }

            var num2 = Dot(normal, tangent);
            tangent -= num2 * normal;
            var num3 = magnitudeStatic(ref tangent);
            if (num3 > (double) Mathf.Epsilon)
            {
                tangent /= num3;
            }
            else
            {
                tangent = orthoNormalVectorFast(ref normal);
            }

            var num4 = Dot(tangent, binormal);
            var num5 = Dot(normal, binormal);
            binormal -= num5 * normal + num4 * tangent;
            var num6 = magnitudeStatic(ref binormal);
            if (num6 > (double) Mathf.Epsilon)
            {
                binormal /= num6;
            }
            else
            {
                binormal = Cross(normal, tangent);
            }
        }

        public static Vector3 Project(Vector3 vector, Vector3 onNormal)
        {
            return onNormal * Dot(vector, onNormal) / Dot(onNormal, onNormal);
        }

        public static void Project(ref Vector3 vector, ref Vector3 onNormal, out Vector3 result)
        {
            result = onNormal * Dot(vector, onNormal) / Dot(onNormal, onNormal);
        }

        public static float Angle(Vector3 from, Vector3 to)
        {
            float num = (float) Math.Sqrt((double) from.sqrMagnitude * (double) to.sqrMagnitude);
            return (double) num < 1.00000000362749E-15 ? 0.0f : (float) Math.Acos((double) Mathf.Clamp(Vector3.Dot(from, to) / num, -1f, 1f)) * 57.29578f;
        }

        public static void Angle(ref Vector3 from, ref Vector3 to, out float result)
        {
            float num = (float) Math.Sqrt((double) from.sqrMagnitude * (double) to.sqrMagnitude);
            result = (double) num < 1.00000000362749E-15 ? 0.0f : (float) Math.Acos((double) Mathf.Clamp(Vector3.Dot(from, to) / num, -1f, 1f)) * 57.29578f;
        }

        public static Vector3 operator -(Vector3 value)
        {
            Vector3 vector3;
            vector3.x = -value.x;
            vector3.y = -value.y;
            vector3.z = -value.z;
            return vector3;
        }

        public static bool operator ==(Vector3 lhs, Vector3 rhs)
        {
            return (lhs - rhs).sqrMagnitude < 9.99999943962493E-11;
        }

        public static bool operator !=(Vector3 lhs, Vector3 rhs)
        {
            return !(lhs == rhs);
        }

        public static Vector3 operator +(Vector3 value1, Vector3 value2)
        {
            Vector3 vector3;
            vector3.x = value1.x + value2.x;
            vector3.y = value1.y + value2.y;
            vector3.z = value1.z + value2.z;
            return vector3;
        }

        public static Vector3 operator -(Vector3 value1, Vector3 value2)
        {
            Vector3 vector3;
            vector3.x = value1.x - value2.x;
            vector3.y = value1.y - value2.y;
            vector3.z = value1.z - value2.z;
            return vector3;
        }

        public static Vector3 operator *(Vector3 value1, Vector3 value2)
        {
            Vector3 vector3;
            vector3.x = value1.x * value2.x;
            vector3.y = value1.y * value2.y;
            vector3.z = value1.z * value2.z;
            return vector3;
        }

        public static Vector3 operator *(Vector3 value, float scaleFactor)
        {
            Vector3 vector3;
            vector3.x = value.x * scaleFactor;
            vector3.y = value.y * scaleFactor;
            vector3.z = value.z * scaleFactor;
            return vector3;
        }

        public static Vector3 operator *(float scaleFactor, Vector3 value)
        {
            Vector3 vector3;
            vector3.x = value.x * scaleFactor;
            vector3.y = value.y * scaleFactor;
            vector3.z = value.z * scaleFactor;
            return vector3;
        }

        public static Vector3 operator /(Vector3 value1, Vector3 value2)
        {
            Vector3 vector3;
            vector3.x = value1.x / value2.x;
            vector3.y = value1.y / value2.y;
            vector3.z = value1.z / value2.z;
            return vector3;
        }

        public static Vector3 operator /(Vector3 value, float divider)
        {
            var num = 1f / divider;
            Vector3 vector3;
            vector3.x = value.x * num;
            vector3.y = value.y * num;
            vector3.z = value.z * num;
            return vector3;
        }

        public static bool OutOfDistance(Vector3 a, Vector3 b, int dis)
        {
            return DistanceSquared(a, b) > dis * dis;
        }
    }
}