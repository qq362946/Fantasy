using System;
using System.Runtime.CompilerServices;

namespace DotRecast.Core.Numerics
{
    public struct RcMatrix4x4f
    {
        private static readonly RcMatrix4x4f _identity = new RcMatrix4x4f
        (
            1f, 0f, 0f, 0f,
            0f, 1f, 0f, 0f,
            0f, 0f, 1f, 0f,
            0f, 0f, 0f, 1f
        );

        public float M11; // 0
        public float M12; // 1
        public float M13; // 2
        public float M14; // 3
        public float M21; // 4
        public float M22; // 5
        public float M23; // 6
        public float M24; // 7 
        public float M31; // 8
        public float M32; // 9
        public float M33; // 10
        public float M34; // 11
        public float M41; // 12
        public float M42; // 13
        public float M43; // 14
        public float M44; // 15

        public RcMatrix4x4f(
            float m11, float m12, float m13, float m14,
            float m21, float m22, float m23, float m24,
            float m31, float m32, float m33, float m34,
            float m41, float m42, float m43, float m44)
        {
            M11 = m11;
            M12 = m12;
            M13 = m13;
            M14 = m14;

            M21 = m21;
            M22 = m22;
            M23 = m23;
            M24 = m24;

            M31 = m31;
            M32 = m32;
            M33 = m33;
            M34 = m34;

            M41 = m41;
            M42 = m42;
            M43 = m43;
            M44 = m44;
        }

        public RcMatrix4x4f(float[] m)
        {
            M11 = m[0];
            M12 = m[1];
            M13 = m[2];
            M14 = m[3];
            M21 = m[4];
            M22 = m[5];
            M23 = m[6];
            M24 = m[7];
            M31 = m[8];
            M32 = m[9];
            M33 = m[10];
            M34 = m[11];
            M41 = m[12];
            M42 = m[13];
            M43 = m[14];
            M44 = m[15];
        }


        public void CopyTo(float[] m)
        {
            m[0] = M11;
            m[1] = M12;
            m[2] = M13;
            m[3] = M14;
            m[4] = M21;
            m[5] = M22;
            m[6] = M23;
            m[7] = M24;
            m[8] = M31;
            m[9] = M32;
            m[10] = M33;
            m[11] = M34;
            m[12] = M41;
            m[13] = M42;
            m[14] = M43;
            m[15] = M44;
        }


        public static RcMatrix4x4f Identity => _identity;

        public readonly bool IsIdentity =>
            M11.Equals(1f) && M22.Equals(1f) && M33.Equals(1f) && M44.Equals(1f) &&
            M12 == 0f && M13 == 0f && M14 == 0f &&
            M21 == 0f && M23 == 0f && M24 == 0f &&
            M31 == 0f && M32 == 0f && M34 == 0f &&
            M41 == 0f && M42 == 0f && M43 == 0f;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static RcMatrix4x4f Mul(ref RcMatrix4x4f left, ref RcMatrix4x4f right)
        {
            float m11 = left.M11 * right.M11 + left.M21 * right.M12 + left.M31 * right.M13 + left.M41 * right.M14;
            float m12 = left.M12 * right.M11 + left.M22 * right.M12 + left.M32 * right.M13 + left.M42 * right.M14;
            float m13 = left.M13 * right.M11 + left.M23 * right.M12 + left.M33 * right.M13 + left.M43 * right.M14;
            float m14 = left.M14 * right.M11 + left.M24 * right.M12 + left.M34 * right.M13 + left.M44 * right.M14;
            float m21 = left.M11 * right.M21 + left.M21 * right.M22 + left.M31 * right.M23 + left.M41 * right.M24;
            float m22 = left.M12 * right.M21 + left.M22 * right.M22 + left.M32 * right.M23 + left.M42 * right.M24;
            float m23 = left.M13 * right.M21 + left.M23 * right.M22 + left.M33 * right.M23 + left.M43 * right.M24;
            float m24 = left.M14 * right.M21 + left.M24 * right.M22 + left.M34 * right.M23 + left.M44 * right.M24;
            float m31 = left.M11 * right.M31 + left.M21 * right.M32 + left.M31 * right.M33 + left.M41 * right.M34;
            float m32 = left.M12 * right.M31 + left.M22 * right.M32 + left.M32 * right.M33 + left.M42 * right.M34;
            float m33 = left.M13 * right.M31 + left.M23 * right.M32 + left.M33 * right.M33 + left.M43 * right.M34;
            float m34 = left.M14 * right.M31 + left.M24 * right.M32 + left.M34 * right.M33 + left.M44 * right.M34;
            float m41 = left.M11 * right.M41 + left.M21 * right.M42 + left.M31 * right.M43 + left.M41 * right.M44;
            float m42 = left.M12 * right.M41 + left.M22 * right.M42 + left.M32 * right.M43 + left.M42 * right.M44;
            float m43 = left.M13 * right.M41 + left.M23 * right.M42 + left.M33 * right.M43 + left.M43 * right.M44;
            float m44 = left.M14 * right.M41 + left.M24 * right.M42 + left.M34 * right.M43 + left.M44 * right.M44;

            RcMatrix4x4f dest = new RcMatrix4x4f();
            dest.M11 = m11;
            dest.M12 = m12;
            dest.M13 = m13;
            dest.M14 = m14;
            dest.M21 = m21;
            dest.M22 = m22;
            dest.M23 = m23;
            dest.M24 = m24;
            dest.M31 = m31;
            dest.M32 = m32;
            dest.M33 = m33;
            dest.M34 = m34;
            dest.M41 = m41;
            dest.M42 = m42;
            dest.M43 = m43;
            dest.M44 = m44;

            return dest;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static RcMatrix4x4f Mul(float[] left, float[] right)
        {
            float m00 = left[0] * right[0] + left[4] * right[1] + left[8] * right[2] + left[12] * right[3];
            float m01 = left[1] * right[0] + left[5] * right[1] + left[9] * right[2] + left[13] * right[3];
            float m02 = left[2] * right[0] + left[6] * right[1] + left[10] * right[2] + left[14] * right[3];
            float m03 = left[3] * right[0] + left[7] * right[1] + left[11] * right[2] + left[15] * right[3];
            float m10 = left[0] * right[4] + left[4] * right[5] + left[8] * right[6] + left[12] * right[7];
            float m11 = left[1] * right[4] + left[5] * right[5] + left[9] * right[6] + left[13] * right[7];
            float m12 = left[2] * right[4] + left[6] * right[5] + left[10] * right[6] + left[14] * right[7];
            float m13 = left[3] * right[4] + left[7] * right[5] + left[11] * right[6] + left[15] * right[7];
            float m20 = left[0] * right[8] + left[4] * right[9] + left[8] * right[10] + left[12] * right[11];
            float m21 = left[1] * right[8] + left[5] * right[9] + left[9] * right[10] + left[13] * right[11];
            float m22 = left[2] * right[8] + left[6] * right[9] + left[10] * right[10] + left[14] * right[11];
            float m23 = left[3] * right[8] + left[7] * right[9] + left[11] * right[10] + left[15] * right[11];
            float m30 = left[0] * right[12] + left[4] * right[13] + left[8] * right[14] + left[12] * right[15];
            float m31 = left[1] * right[12] + left[5] * right[13] + left[9] * right[14] + left[13] * right[15];
            float m32 = left[2] * right[12] + left[6] * right[13] + left[10] * right[14] + left[14] * right[15];
            float m33 = left[3] * right[12] + left[7] * right[13] + left[11] * right[14] + left[15] * right[15];

            return new RcMatrix4x4f(
                m00, m01, m02, m03,
                m10, m11, m12, m13,
                m20, m21, m22, m23,
                m30, m31, m32, m33
            );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static RcMatrix4x4f CreateFromRotate(float a, float x, float y, float z)
        {
            var matrix = new RcMatrix4x4f();
            a = (float)(a * MathF.PI / 180.0); // convert to radians
            float s = MathF.Sin(a);
            float c = MathF.Cos(a);
            float t = 1.0f - c;

            float tx = t * x;
            float ty = t * y;
            float tz = t * z;

            float sz = s * z;
            float sy = s * y;
            float sx = s * x;

            matrix.M11 = tx * x + c;
            matrix.M12 = tx * y + sz;
            matrix.M13 = tx * z - sy;
            matrix.M14 = 0;

            matrix.M21 = tx * y - sz;
            matrix.M22 = ty * y + c;
            matrix.M23 = ty * z + sx;
            matrix.M24 = 0;

            matrix.M31 = tx * z + sy;
            matrix.M32 = ty * z - sx;
            matrix.M33 = tz * z + c;
            matrix.M34 = 0;

            matrix.M41 = 0;
            matrix.M42 = 0;
            matrix.M43 = 0;
            matrix.M44 = 1;

            return matrix;
        }
    }
}