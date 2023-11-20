using System;
using System.Globalization;

namespace UnityEngine
{
    [Serializable]
    public struct Matrix4x4 : IEquatable<Matrix4x4>
    {
        public static readonly Matrix4x4 identity = new(1f, 0.0f, 0.0f, 0.0f, 0.0f, 1f, 0.0f, 0.0f, 0.0f, 0.0f, 1f,
            0.0f, 0.0f, 0.0f, 0.0f, 1f);

        public float m00;
        public float m01;
        public float m02;
        public float m03;
        public float m10;
        public float m11;
        public float m12;
        public float m13;
        public float m20;
        public float m21;
        public float m22;
        public float m23;
        public float m30;
        public float m31;
        public float m32;
        public float m33;

        public Matrix4x4(float m00, float m01, float m02, float m03, float m10, float m11, float m12, float m13,
            float m20, float m21, float m22, float m23, float m30, float m31, float m32, float m33)
        {
            this.m00 = m00;
            this.m01 = m01;
            this.m02 = m02;
            this.m03 = m03;
            this.m10 = m10;
            this.m11 = m11;
            this.m12 = m12;
            this.m13 = m13;
            this.m20 = m20;
            this.m21 = m21;
            this.m22 = m22;
            this.m23 = m23;
            this.m30 = m30;
            this.m31 = m31;
            this.m32 = m32;
            this.m33 = m33;
        }

        public bool isIdentity =>
            m00 == 1f && m11 == 1f && m22 == 1f && m33 == 1f && // Check diagonal element first for early out.
            m12 == 0.0f && m13 == 0.0f && m13 == 0.0f && m21 == 0.0f && m23 == 0.0f && m23 == 0.0f && m31 == 0.0f &&
            m32 == 0.0f && m33 == 0.0f;

        public Vector3 up
        {
            get
            {
                Vector3 vector3;
                vector3.x = m01;
                vector3.y = m11;
                vector3.z = m21;
                return vector3;
            }
            set
            {
                m01 = value.x;
                m11 = value.y;
                m21 = value.z;
            }
        }

        public Vector3 down
        {
            get
            {
                Vector3 vector3;
                vector3.x = -m01;
                vector3.y = -m11;
                vector3.z = -m21;
                return vector3;
            }
            set
            {
                m01 = -value.x;
                m11 = -value.y;
                m21 = -value.z;
            }
        }

        public Vector3 right
        {
            get
            {
                Vector3 vector3;
                vector3.x = m00;
                vector3.y = m10;
                vector3.z = m20;
                return vector3;
            }
            set
            {
                m00 = value.x;
                m10 = value.y;
                m20 = value.z;
            }
        }

        public Vector3 left
        {
            get
            {
                Vector3 vector3;
                vector3.x = -m00;
                vector3.y = -m10;
                vector3.z = -m20;
                return vector3;
            }
            set
            {
                m00 = -value.x;
                m10 = -value.y;
                m20 = -value.z;
            }
        }

        public Vector3 forward
        {
            get
            {
                Vector3 vector3;
                vector3.x = -m02;
                vector3.y = -m12;
                vector3.z = -m22;
                return vector3;
            }
            set
            {
                m02 = -value.x;
                m12 = -value.y;
                m22 = -value.z;
            }
        }

        public Vector3 back
        {
            get
            {
                Vector3 vector3;
                vector3.x = m02;
                vector3.y = m12;
                vector3.z = m22;
                return vector3;
            }
            set
            {
                m02 = value.x;
                m12 = value.y;
                m22 = value.z;
            }
        }

        public unsafe float this[int row, int col]
        {
            get
            {
                fixed (float* numPtr = &m00)
                {
                    return numPtr[row * 4 + col];
                }
            }
            set
            {
                fixed (float* numPtr = &m00)
                {
                    numPtr[row * 4 + col] = value;
                }
            }
        }

        public unsafe float this[int index]
        {
            get
            {
                fixed (float* numPtr = &m00)
                {
                    return numPtr[index];
                }
            }
            set
            {
                fixed (float* numPtr = &m00)
                {
                    numPtr[index] = value;
                }
            }
        }

        public Matrix4x4 inverse => Invert(this);

        public bool Equals(Matrix4x4 other)
        {
            if (m00 == (double) other.m00 && m11 == (double) other.m11 && m22 == (double) other.m22 &&
                m33 == (double) other.m33 && m01 == (double) other.m01 && m02 == (double) other.m02 &&
                m03 == (double) other.m03 && m10 == (double) other.m10 && m12 == (double) other.m12 &&
                m13 == (double) other.m13 && m20 == (double) other.m20 && m21 == (double) other.m21 &&
                m23 == (double) other.m23 && m30 == (double) other.m30 && m31 == (double) other.m31)
            {
                return m32 == (double) other.m32;
            }

            return false;
        }

        public Vector4 GetRow(int index)
        {
            Vector4 vector4;
            vector4.x = this[index, 0];
            vector4.y = this[index, 1];
            vector4.z = this[index, 2];
            vector4.w = this[index, 3];
            return vector4;
        }

        public void SetRow(int index, Vector4 value)
        {
            this[index, 0] = value.x;
            this[index, 1] = value.y;
            this[index, 2] = value.z;
            this[index, 3] = value.w;
        }

        public Vector4 GetColumn(int index)
        {
            Vector4 vector4;
            vector4.x = this[0, index];
            vector4.y = this[1, index];
            vector4.z = this[2, index];
            vector4.w = this[3, index];
            return vector4;
        }

        public void SetColumn(int index, Vector4 value)
        {
            this[0, index] = value.x;
            this[1, index] = value.y;
            this[2, index] = value.z;
            this[3, index] = value.w;
        }

        public static Matrix4x4 CreateTranslation(Vector3 position)
        {
            Matrix4x4 matrix44;
            matrix44.m00 = 1f;
            matrix44.m01 = 0.0f;
            matrix44.m02 = 0.0f;
            matrix44.m03 = position.x;
            matrix44.m10 = 0.0f;
            matrix44.m11 = 1f;
            matrix44.m12 = 0.0f;
            matrix44.m13 = position.y;
            matrix44.m20 = 0.0f;
            matrix44.m21 = 0.0f;
            matrix44.m22 = 1f;
            matrix44.m23 = position.z;
            matrix44.m30 = 0.0f;
            matrix44.m31 = 0.0f;
            matrix44.m32 = 0.0f;
            matrix44.m33 = 1f;
            return matrix44;
        }

        public static void CreateTranslation(ref Vector3 position, out Matrix4x4 matrix)
        {
            matrix.m00 = 1f;
            matrix.m01 = 0.0f;
            matrix.m02 = 0.0f;
            matrix.m03 = position.x;
            matrix.m10 = 0.0f;
            matrix.m11 = 1f;
            matrix.m12 = 0.0f;
            matrix.m13 = position.y;
            matrix.m20 = 0.0f;
            matrix.m21 = 0.0f;
            matrix.m22 = 1f;
            matrix.m23 = position.z;
            matrix.m30 = 0.0f;
            matrix.m31 = 0.0f;
            matrix.m32 = 0.0f;
            matrix.m33 = 1f;
        }

        public static Matrix4x4 CreateScale(Vector3 scales)
        {
            Matrix4x4 matrix44;
            matrix44.m00 = scales.x;
            matrix44.m01 = 0.0f;
            matrix44.m02 = 0.0f;
            matrix44.m03 = 0.0f;
            matrix44.m10 = 0.0f;
            matrix44.m11 = scales.y;
            matrix44.m12 = 0.0f;
            matrix44.m13 = 0.0f;
            matrix44.m20 = 0.0f;
            matrix44.m21 = 0.0f;
            matrix44.m22 = scales.z;
            matrix44.m23 = 0.0f;
            matrix44.m30 = 0.0f;
            matrix44.m31 = 0.0f;
            matrix44.m32 = 0.0f;
            matrix44.m33 = 1f;
            return matrix44;
        }

        public static Matrix4x4 TRS(Vector3 pos, Quaternion q, Vector3 s)
        {
            var m1 = CreateTranslation(pos);
            var m2 = CreateFromQuaternion(q);
            var m3 = CreateScale(s);
            return m1 * m2 * m3;
        }

        public static Matrix4x4 Scale(Vector3 scales)
        {
            Matrix4x4 m1;
            CreateScale(ref scales, out m1);
            return m1;
        }

        public static void CreateScale(ref Vector3 scales, out Matrix4x4 matrix)
        {
            matrix.m00 = scales.x;
            matrix.m01 = 0.0f;
            matrix.m02 = 0.0f;
            matrix.m03 = 0.0f;
            matrix.m10 = 0.0f;
            matrix.m11 = scales.y;
            matrix.m12 = 0.0f;
            matrix.m13 = 0.0f;
            matrix.m20 = 0.0f;
            matrix.m21 = 0.0f;
            matrix.m22 = scales.z;
            matrix.m23 = 0.0f;
            matrix.m30 = 0.0f;
            matrix.m31 = 0.0f;
            matrix.m32 = 0.0f;
            matrix.m33 = 1f;
        }

        public static Matrix4x4 CreateOrthographic(float width, float height, float zNearPlane, float zFarPlane)
        {
            Matrix4x4 matrix44;
            matrix44.m00 = 2f / width;
            matrix44.m10 = matrix44.m20 = matrix44.m30 = 0.0f;
            matrix44.m11 = 2f / height;
            matrix44.m01 = matrix44.m21 = matrix44.m31 = 0.0f;
            matrix44.m22 = (float) (1.0 / (zNearPlane - (double) zFarPlane));
            matrix44.m02 = matrix44.m12 = matrix44.m32 = 0.0f;
            matrix44.m03 = matrix44.m13 = 0.0f;
            matrix44.m23 = zNearPlane / (zNearPlane - zFarPlane);
            matrix44.m33 = 1f;
            return matrix44;
        }

        public static void CreateOrthographic(float width, float height, float zNearPlane, float zFarPlane,
            out Matrix4x4 matrix)
        {
            matrix.m00 = 2f / width;
            matrix.m10 = matrix.m20 = matrix.m30 = 0.0f;
            matrix.m11 = 2f / height;
            matrix.m01 = matrix.m21 = matrix.m31 = 0.0f;
            matrix.m22 = (float) (1.0 / (zNearPlane - (double) zFarPlane));
            matrix.m02 = matrix.m12 = matrix.m32 = 0.0f;
            matrix.m03 = matrix.m13 = 0.0f;
            matrix.m23 = zNearPlane / (zNearPlane - zFarPlane);
            matrix.m33 = 1f;
        }

        public static Matrix4x4 CreateLookAt(Vector3 cameraPosition, Vector3 cameraTarget, Vector3 cameraUpVector)
        {
            var vector3_1 = Vector3.Normalize(cameraPosition - cameraTarget);
            var vector3_2 = Vector3.Normalize(Vector3.Cross(cameraUpVector, vector3_1));
            var vector1 = Vector3.Cross(vector3_1, vector3_2);
            Matrix4x4 matrix44;
            matrix44.m00 = vector3_2.x;
            matrix44.m10 = vector1.x;
            matrix44.m20 = vector3_1.x;
            matrix44.m30 = 0.0f;
            matrix44.m01 = vector3_2.y;
            matrix44.m11 = vector1.y;
            matrix44.m21 = vector3_1.y;
            matrix44.m31 = 0.0f;
            matrix44.m02 = vector3_2.z;
            matrix44.m12 = vector1.z;
            matrix44.m22 = vector3_1.z;
            matrix44.m32 = 0.0f;
            matrix44.m03 = -Vector3.Dot(vector3_2, cameraPosition);
            matrix44.m13 = -Vector3.Dot(vector1, cameraPosition);
            matrix44.m23 = -Vector3.Dot(vector3_1, cameraPosition);
            matrix44.m33 = 1f;
            return matrix44;
        }

        public static void CreateLookAt(ref Vector3 cameraPosition, ref Vector3 cameraTarget,
            ref Vector3 cameraUpVector, out Matrix4x4 matrix)
        {
            var vector3_1 = Vector3.Normalize(cameraPosition - cameraTarget);
            var vector3_2 = Vector3.Normalize(Vector3.Cross(cameraUpVector, vector3_1));
            var vector1 = Vector3.Cross(vector3_1, vector3_2);
            matrix.m00 = vector3_2.x;
            matrix.m10 = vector1.x;
            matrix.m20 = vector3_1.x;
            matrix.m30 = 0.0f;
            matrix.m01 = vector3_2.y;
            matrix.m11 = vector1.y;
            matrix.m21 = vector3_1.y;
            matrix.m31 = 0.0f;
            matrix.m02 = vector3_2.z;
            matrix.m12 = vector1.z;
            matrix.m22 = vector3_1.z;
            matrix.m32 = 0.0f;
            matrix.m03 = -Vector3.Dot(vector3_2, cameraPosition);
            matrix.m13 = -Vector3.Dot(vector1, cameraPosition);
            matrix.m23 = -Vector3.Dot(vector3_1, cameraPosition);
            matrix.m33 = 1f;
        }

        public static Matrix4x4 CreateFromQuaternion(Quaternion quaternion)
        {
            var num1 = quaternion.x * quaternion.x;
            var num2 = quaternion.y * quaternion.y;
            var num3 = quaternion.z * quaternion.z;
            var num4 = quaternion.x * quaternion.y;
            var num5 = quaternion.z * quaternion.w;
            var num6 = quaternion.z * quaternion.x;
            var num7 = quaternion.y * quaternion.w;
            var num8 = quaternion.y * quaternion.z;
            var num9 = quaternion.x * quaternion.w;
            Matrix4x4 matrix44;
            matrix44.m00 = (float) (1.0 - 2.0 * (num2 + (double) num3));
            matrix44.m10 = (float) (2.0 * (num4 + (double) num5));
            matrix44.m20 = (float) (2.0 * (num6 - (double) num7));
            matrix44.m30 = 0.0f;
            matrix44.m01 = (float) (2.0 * (num4 - (double) num5));
            matrix44.m11 = (float) (1.0 - 2.0 * (num3 + (double) num1));
            matrix44.m21 = (float) (2.0 * (num8 + (double) num9));
            matrix44.m31 = 0.0f;
            matrix44.m02 = (float) (2.0 * (num6 + (double) num7));
            matrix44.m12 = (float) (2.0 * (num8 - (double) num9));
            matrix44.m22 = (float) (1.0 - 2.0 * (num2 + (double) num1));
            matrix44.m32 = 0.0f;
            matrix44.m03 = 0.0f;
            matrix44.m13 = 0.0f;
            matrix44.m23 = 0.0f;
            matrix44.m33 = 1f;
            return matrix44;
        }

        public static void CreateFromQuaternion(ref Quaternion quaternion, out Matrix4x4 matrix)
        {
            var num1 = quaternion.x * quaternion.x;
            var num2 = quaternion.y * quaternion.y;
            var num3 = quaternion.z * quaternion.z;
            var num4 = quaternion.x * quaternion.y;
            var num5 = quaternion.z * quaternion.w;
            var num6 = quaternion.z * quaternion.x;
            var num7 = quaternion.y * quaternion.w;
            var num8 = quaternion.y * quaternion.z;
            var num9 = quaternion.x * quaternion.w;
            matrix.m00 = (float) (1.0 - 2.0 * (num2 + (double) num3));
            matrix.m10 = (float) (2.0 * (num4 + (double) num5));
            matrix.m20 = (float) (2.0 * (num6 - (double) num7));
            matrix.m30 = 0.0f;
            matrix.m01 = (float) (2.0 * (num4 - (double) num5));
            matrix.m11 = (float) (1.0 - 2.0 * (num3 + (double) num1));
            matrix.m21 = (float) (2.0 * (num8 + (double) num9));
            matrix.m31 = 0.0f;
            matrix.m02 = (float) (2.0 * (num6 + (double) num7));
            matrix.m12 = (float) (2.0 * (num8 - (double) num9));
            matrix.m22 = (float) (1.0 - 2.0 * (num2 + (double) num1));
            matrix.m32 = 0.0f;
            matrix.m03 = 0.0f;
            matrix.m13 = 0.0f;
            matrix.m23 = 0.0f;
            matrix.m33 = 1f;
        }

        public static Matrix4x4 CreateFromYawPitchRoll(float yaw, float pitch, float roll)
        {
            Quaternion result;
            Quaternion.CreateFromYawPitchRoll(yaw, pitch, roll, out result);
            return CreateFromQuaternion(result);
        }

        public static void CreateFromYawPitchRoll(float yaw, float pitch, float roll, out Matrix4x4 result)
        {
            Quaternion result1;
            Quaternion.CreateFromYawPitchRoll(yaw, pitch, roll, out result1);
            result = CreateFromQuaternion(result1);
        }

        public static Matrix4x4 CreateRotationX(float radians)
        {
            var num1 = (float) Math.Cos(radians);
            var num2 = (float) Math.Sin(radians);
            Matrix4x4 matrix44;
            matrix44.m00 = 1f;
            matrix44.m10 = 0.0f;
            matrix44.m20 = 0.0f;
            matrix44.m30 = 0.0f;
            matrix44.m01 = 0.0f;
            matrix44.m11 = num1;
            matrix44.m21 = num2;
            matrix44.m31 = 0.0f;
            matrix44.m02 = 0.0f;
            matrix44.m12 = -num2;
            matrix44.m22 = num1;
            matrix44.m32 = 0.0f;
            matrix44.m03 = 0.0f;
            matrix44.m13 = 0.0f;
            matrix44.m23 = 0.0f;
            matrix44.m33 = 1f;
            return matrix44;
        }

        public static void CreateRotationX(float radians, out Matrix4x4 result)
        {
            var num1 = (float) Math.Cos(radians);
            var num2 = (float) Math.Sin(radians);
            result.m00 = 1f;
            result.m10 = 0.0f;
            result.m20 = 0.0f;
            result.m30 = 0.0f;
            result.m01 = 0.0f;
            result.m11 = num1;
            result.m21 = num2;
            result.m31 = 0.0f;
            result.m02 = 0.0f;
            result.m12 = -num2;
            result.m22 = num1;
            result.m32 = 0.0f;
            result.m03 = 0.0f;
            result.m13 = 0.0f;
            result.m23 = 0.0f;
            result.m33 = 1f;
        }

        public static Matrix4x4 CreateRotationY(float radians)
        {
            var num1 = (float) Math.Cos(radians);
            var num2 = (float) Math.Sin(radians);
            Matrix4x4 matrix44;
            matrix44.m00 = num1;
            matrix44.m10 = 0.0f;
            matrix44.m20 = -num2;
            matrix44.m30 = 0.0f;
            matrix44.m01 = 0.0f;
            matrix44.m11 = 1f;
            matrix44.m21 = 0.0f;
            matrix44.m31 = 0.0f;
            matrix44.m02 = num2;
            matrix44.m12 = 0.0f;
            matrix44.m22 = num1;
            matrix44.m32 = 0.0f;
            matrix44.m03 = 0.0f;
            matrix44.m13 = 0.0f;
            matrix44.m23 = 0.0f;
            matrix44.m33 = 1f;
            return matrix44;
        }

        public static void CreateRotationY(float radians, out Matrix4x4 result)
        {
            var num1 = (float) Math.Cos(radians);
            var num2 = (float) Math.Sin(radians);
            result.m00 = num1;
            result.m10 = 0.0f;
            result.m20 = -num2;
            result.m30 = 0.0f;
            result.m01 = 0.0f;
            result.m11 = 1f;
            result.m21 = 0.0f;
            result.m31 = 0.0f;
            result.m02 = num2;
            result.m12 = 0.0f;
            result.m22 = num1;
            result.m32 = 0.0f;
            result.m03 = 0.0f;
            result.m13 = 0.0f;
            result.m23 = 0.0f;
            result.m33 = 1f;
        }

        public static Matrix4x4 CreateRotationZ(float radians)
        {
            var num1 = (float) Math.Cos(radians);
            var num2 = (float) Math.Sin(radians);
            Matrix4x4 matrix44;
            matrix44.m00 = num1;
            matrix44.m10 = num2;
            matrix44.m20 = 0.0f;
            matrix44.m30 = 0.0f;
            matrix44.m01 = -num2;
            matrix44.m11 = num1;
            matrix44.m21 = 0.0f;
            matrix44.m31 = 0.0f;
            matrix44.m02 = 0.0f;
            matrix44.m12 = 0.0f;
            matrix44.m22 = 1f;
            matrix44.m32 = 0.0f;
            matrix44.m03 = 0.0f;
            matrix44.m13 = 0.0f;
            matrix44.m23 = 0.0f;
            matrix44.m33 = 1f;
            return matrix44;
        }

        public static void CreateRotationZ(float radians, out Matrix4x4 result)
        {
            var num1 = (float) Math.Cos(radians);
            var num2 = (float) Math.Sin(radians);
            result.m00 = num1;
            result.m10 = num2;
            result.m20 = 0.0f;
            result.m30 = 0.0f;
            result.m01 = -num2;
            result.m11 = num1;
            result.m21 = 0.0f;
            result.m31 = 0.0f;
            result.m02 = 0.0f;
            result.m12 = 0.0f;
            result.m22 = 1f;
            result.m32 = 0.0f;
            result.m03 = 0.0f;
            result.m13 = 0.0f;
            result.m23 = 0.0f;
            result.m33 = 1f;
        }

        public static Matrix4x4 CreateFromAxisAngle(Vector3 axis, float angle)
        {
            var x = axis.x;
            var y = axis.y;
            var z = axis.z;
            var num1 = (float) Math.Sin(angle);
            var num2 = (float) Math.Cos(angle);
            var num3 = x * x;
            var num4 = y * y;
            var num5 = z * z;
            var num6 = x * y;
            var num7 = x * z;
            var num8 = y * z;
            Matrix4x4 matrix44;
            matrix44.m00 = num3 + num2 * (1f - num3);
            matrix44.m10 = (float) (num6 - num2 * (double) num6 + num1 * (double) z);
            matrix44.m20 = (float) (num7 - num2 * (double) num7 - num1 * (double) y);
            matrix44.m30 = 0.0f;
            matrix44.m01 = (float) (num6 - num2 * (double) num6 - num1 * (double) z);
            matrix44.m11 = num4 + num2 * (1f - num4);
            matrix44.m21 = (float) (num8 - num2 * (double) num8 + num1 * (double) x);
            matrix44.m31 = 0.0f;
            matrix44.m02 = (float) (num7 - num2 * (double) num7 + num1 * (double) y);
            matrix44.m12 = (float) (num8 - num2 * (double) num8 - num1 * (double) x);
            matrix44.m22 = num5 + num2 * (1f - num5);
            matrix44.m32 = 0.0f;
            matrix44.m03 = 0.0f;
            matrix44.m13 = 0.0f;
            matrix44.m23 = 0.0f;
            matrix44.m33 = 1f;
            return matrix44;
        }

        public static void CreateFromAxisAngle(ref Vector3 axis, float angle, out Matrix4x4 result)
        {
            var x = axis.x;
            var y = axis.y;
            var z = axis.z;
            var num1 = (float) Math.Sin(angle);
            var num2 = (float) Math.Cos(angle);
            var num3 = x * x;
            var num4 = y * y;
            var num5 = z * z;
            var num6 = x * y;
            var num7 = x * z;
            var num8 = y * z;
            result.m00 = num3 + num2 * (1f - num3);
            result.m10 = (float) (num6 - num2 * (double) num6 + num1 * (double) z);
            result.m20 = (float) (num7 - num2 * (double) num7 - num1 * (double) y);
            result.m30 = 0.0f;
            result.m01 = (float) (num6 - num2 * (double) num6 - num1 * (double) z);
            result.m11 = num4 + num2 * (1f - num4);
            result.m21 = (float) (num8 - num2 * (double) num8 + num1 * (double) x);
            result.m31 = 0.0f;
            result.m02 = (float) (num7 - num2 * (double) num7 + num1 * (double) y);
            result.m12 = (float) (num8 - num2 * (double) num8 - num1 * (double) x);
            result.m22 = num5 + num2 * (1f - num5);
            result.m32 = 0.0f;
            result.m03 = 0.0f;
            result.m13 = 0.0f;
            result.m23 = 0.0f;
            result.m33 = 1f;
        }

        public void Decompose(out Vector3 scale, out Quaternion rotation, out Vector3 translation)
        {
            var identity = Matrix4x4.identity;
            var num1 = 1f / (float) Math.Sqrt(this[0, 0] * (double) this[0, 0] + this[1, 0] * (double) this[1, 0] +
                                              this[2, 0] * (double) this[2, 0]);
            identity[0, 0] = this[0, 0] * num1;
            identity[1, 0] = this[1, 0] * num1;
            identity[2, 0] = this[2, 0] * num1;
            var num2 = (float) (identity[0, 0] * (double) this[0, 1] + identity[1, 0] * (double) this[1, 1] +
                                identity[2, 0] * (double) this[2, 1]);
            identity[0, 1] = this[0, 1] - num2 * identity[0, 0];
            identity[1, 1] = this[1, 1] - num2 * identity[1, 0];
            identity[2, 1] = this[2, 1] - num2 * identity[2, 0];
            var num3 = 1f / (float) Math.Sqrt(identity[0, 1] * (double) identity[0, 1] +
                                              identity[1, 1] * (double) identity[1, 1] +
                                              identity[2, 1] * (double) identity[2, 1]);
            identity[0, 1] *= num3;
            identity[1, 1] *= num3;
            identity[2, 1] *= num3;
            var num4 = (float) (identity[0, 0] * (double) this[0, 2] + identity[1, 0] * (double) this[1, 2] +
                                identity[2, 0] * (double) this[2, 2]);
            identity[0, 2] = this[0, 2] - num4 * identity[0, 0];
            identity[1, 2] = this[1, 2] - num4 * identity[1, 0];
            identity[2, 2] = this[2, 2] - num4 * identity[2, 0];
            var num5 = (float) (identity[0, 1] * (double) this[0, 2] + identity[1, 1] * (double) this[1, 2] +
                                identity[2, 1] * (double) this[2, 2]);
            identity[0, 2] -= num5 * identity[0, 1];
            identity[1, 2] -= num5 * identity[1, 1];
            identity[2, 2] -= num5 * identity[2, 1];
            var num6 = 1f / (float) Math.Sqrt(identity[0, 2] * (double) identity[0, 2] +
                                              identity[1, 2] * (double) identity[1, 2] +
                                              identity[2, 2] * (double) identity[2, 2]);
            identity[0, 2] *= num6;
            identity[1, 2] *= num6;
            identity[2, 2] *= num6;
            if (identity[0, 0] * (double) identity[1, 1] * identity[2, 2] +
                identity[0, 1] * (double) identity[1, 2] * identity[2, 0] +
                identity[0, 2] * (double) identity[1, 0] * identity[2, 1] -
                identity[0, 2] * (double) identity[1, 1] * identity[2, 0] -
                identity[0, 1] * (double) identity[1, 0] * identity[2, 2] -
                identity[0, 0] * (double) identity[1, 2] * identity[2, 1] < 0.0)
            {
                for (var index1 = 0 ; index1 < 3 ; ++index1)
                for (var index2 = 0 ; index2 < 3 ; ++index2)
                {
                    identity[index1, index2] = -identity[index1, index2];
                }
            }

            scale = new Vector3(
                (float) (identity[0, 0] * (double) this[0, 0] + identity[1, 0] * (double) this[1, 0] +
                         identity[2, 0] * (double) this[2, 0]),
                (float) (identity[0, 1] * (double) this[0, 1] + identity[1, 1] * (double) this[1, 1] +
                         identity[2, 1] * (double) this[2, 1]),
                (float) (identity[0, 2] * (double) this[0, 2] + identity[1, 2] * (double) this[1, 2] +
                         identity[2, 2] * (double) this[2, 2]));
            rotation = Quaternion.CreateFromRotationMatrix(identity);
            translation = new Vector3(this[0, 3], this[1, 3], this[2, 3]);
        }

        public override string ToString()
        {
            var currentCulture = CultureInfo.CurrentCulture;
            return string.Format(currentCulture, "{0}, {1}, {2}, {3}; ", m00.ToString(currentCulture),
                       m01.ToString(currentCulture), m02.ToString(currentCulture), m03.ToString(currentCulture)) +
                   string.Format(currentCulture, "{0}, {1}, {2}, {3}; ", m10.ToString(currentCulture),
                       m11.ToString(currentCulture), m12.ToString(currentCulture), m13.ToString(currentCulture)) +
                   string.Format(currentCulture, "{0}, {1}, {2}, {3}; ", m20.ToString(currentCulture),
                       m21.ToString(currentCulture), m22.ToString(currentCulture), m23.ToString(currentCulture)) +
                   string.Format(currentCulture, "{0}, {1}, {2}, {3}", m30.ToString(currentCulture),
                       m31.ToString(currentCulture), m32.ToString(currentCulture), m33.ToString(currentCulture));
        }

        public override bool Equals(object obj)
        {
            var flag = false;
            if (obj is Matrix4x4)
            {
                flag = Equals((Matrix4x4) obj);
            }

            return flag;
        }

        public override int GetHashCode()
        {
            return m00.GetHashCode() + m01.GetHashCode() + m02.GetHashCode() + m03.GetHashCode() + m10.GetHashCode() +
                   m11.GetHashCode() + m12.GetHashCode() + m13.GetHashCode() + m20.GetHashCode() + m21.GetHashCode() +
                   m22.GetHashCode() + m23.GetHashCode() + m30.GetHashCode() + m31.GetHashCode() + m32.GetHashCode() +
                   m33.GetHashCode();
        }

        public static Matrix4x4 Transpose(Matrix4x4 matrix)
        {
            Matrix4x4 matrix44;
            matrix44.m00 = matrix.m00;
            matrix44.m01 = matrix.m10;
            matrix44.m02 = matrix.m20;
            matrix44.m03 = matrix.m30;
            matrix44.m10 = matrix.m01;
            matrix44.m11 = matrix.m11;
            matrix44.m12 = matrix.m21;
            matrix44.m13 = matrix.m31;
            matrix44.m20 = matrix.m02;
            matrix44.m21 = matrix.m12;
            matrix44.m22 = matrix.m22;
            matrix44.m23 = matrix.m32;
            matrix44.m30 = matrix.m03;
            matrix44.m31 = matrix.m13;
            matrix44.m32 = matrix.m23;
            matrix44.m33 = matrix.m33;
            return matrix44;
        }

        public static void Transpose(ref Matrix4x4 matrix, out Matrix4x4 result)
        {
            result.m00 = matrix.m00;
            result.m01 = matrix.m10;
            result.m02 = matrix.m20;
            result.m03 = matrix.m30;
            result.m10 = matrix.m01;
            result.m11 = matrix.m11;
            result.m12 = matrix.m21;
            result.m13 = matrix.m31;
            result.m20 = matrix.m02;
            result.m21 = matrix.m12;
            result.m22 = matrix.m22;
            result.m23 = matrix.m32;
            result.m30 = matrix.m03;
            result.m31 = matrix.m13;
            result.m32 = matrix.m23;
            result.m33 = matrix.m33;
        }

        public float Determinant()
        {
            var m00 = this.m00;
            var m10 = this.m10;
            var m20 = this.m20;
            var m30 = this.m30;
            var m01 = this.m01;
            var m11 = this.m11;
            var m21 = this.m21;
            var m31 = this.m31;
            var m02 = this.m02;
            var m12 = this.m12;
            var m22 = this.m22;
            var m32 = this.m32;
            var m03 = this.m03;
            var m13 = this.m13;
            var m23 = this.m23;
            var m33 = this.m33;
            var num1 = (float) (m22 * (double) m33 - m32 * (double) m23);
            var num2 = (float) (m12 * (double) m33 - m32 * (double) m13);
            var num3 = (float) (m12 * (double) m23 - m22 * (double) m13);
            var num4 = (float) (m02 * (double) m33 - m32 * (double) m03);
            var num5 = (float) (m02 * (double) m23 - m22 * (double) m03);
            var num6 = (float) (m02 * (double) m13 - m12 * (double) m03);
            return (float) (m00 * (m11 * (double) num1 - m21 * (double) num2 + m31 * (double) num3) -
                            m10 * (m01 * (double) num1 - m21 * (double) num4 + m31 * (double) num5) +
                            m20 * (m01 * (double) num2 - m11 * (double) num4 + m31 * (double) num6) -
                            m30 * (m01 * (double) num3 - m11 * (double) num5 + m21 * (double) num6));
        }

        public static Matrix4x4 Invert(Matrix4x4 matrix)
        {
            var m00 = matrix.m00;
            var m10 = matrix.m10;
            var m20 = matrix.m20;
            var m30 = matrix.m30;
            var m01 = matrix.m01;
            var m11 = matrix.m11;
            var m21 = matrix.m21;
            var m31 = matrix.m31;
            var m02 = matrix.m02;
            var m12 = matrix.m12;
            var m22 = matrix.m22;
            var m32 = matrix.m32;
            var m03 = matrix.m03;
            var m13 = matrix.m13;
            var m23 = matrix.m23;
            var m33 = matrix.m33;
            var num1 = (float) (m22 * (double) m33 - m32 * (double) m23);
            var num2 = (float) (m12 * (double) m33 - m32 * (double) m13);
            var num3 = (float) (m12 * (double) m23 - m22 * (double) m13);
            var num4 = (float) (m02 * (double) m33 - m32 * (double) m03);
            var num5 = (float) (m02 * (double) m23 - m22 * (double) m03);
            var num6 = (float) (m02 * (double) m13 - m12 * (double) m03);
            var num7 = (float) (m11 * (double) num1 - m21 * (double) num2 + m31 * (double) num3);
            var num8 = (float) -(m01 * (double) num1 - m21 * (double) num4 + m31 * (double) num5);
            var num9 = (float) (m01 * (double) num2 - m11 * (double) num4 + m31 * (double) num6);
            var num10 = (float) -(m01 * (double) num3 - m11 * (double) num5 + m21 * (double) num6);
            var num11 = (float) (1.0 / (m00 * (double) num7 + m10 * (double) num8 + m20 * (double) num9 +
                                        m30 * (double) num10));
            Matrix4x4 matrix44;
            matrix44.m00 = num7 * num11;
            matrix44.m01 = num8 * num11;
            matrix44.m02 = num9 * num11;
            matrix44.m03 = num10 * num11;
            matrix44.m10 = (float) -(m10 * (double) num1 - m20 * (double) num2 + m30 * (double) num3) * num11;
            matrix44.m11 = (float) (m00 * (double) num1 - m20 * (double) num4 + m30 * (double) num5) * num11;
            matrix44.m12 = (float) -(m00 * (double) num2 - m10 * (double) num4 + m30 * (double) num6) * num11;
            matrix44.m13 = (float) (m00 * (double) num3 - m10 * (double) num5 + m20 * (double) num6) * num11;
            var num12 = (float) (m21 * (double) m33 - m31 * (double) m23);
            var num13 = (float) (m11 * (double) m33 - m31 * (double) m13);
            var num14 = (float) (m11 * (double) m23 - m21 * (double) m13);
            var num15 = (float) (m01 * (double) m33 - m31 * (double) m03);
            var num16 = (float) (m01 * (double) m23 - m21 * (double) m03);
            var num17 = (float) (m01 * (double) m13 - m11 * (double) m03);
            matrix44.m20 = (float) (m10 * (double) num12 - m20 * (double) num13 + m30 * (double) num14) * num11;
            matrix44.m21 = (float) -(m00 * (double) num12 - m20 * (double) num15 + m30 * (double) num16) * num11;
            matrix44.m22 = (float) (m00 * (double) num13 - m10 * (double) num15 + m30 * (double) num17) * num11;
            matrix44.m23 = (float) -(m00 * (double) num14 - m10 * (double) num16 + m20 * (double) num17) * num11;
            var num18 = (float) (m21 * (double) m32 - m31 * (double) m22);
            var num19 = (float) (m11 * (double) m32 - m31 * (double) m12);
            var num20 = (float) (m11 * (double) m22 - m21 * (double) m12);
            var num21 = (float) (m01 * (double) m32 - m31 * (double) m02);
            var num22 = (float) (m01 * (double) m22 - m21 * (double) m02);
            var num23 = (float) (m01 * (double) m12 - m11 * (double) m02);
            matrix44.m30 = (float) -(m10 * (double) num18 - m20 * (double) num19 + m30 * (double) num20) * num11;
            matrix44.m31 = (float) (m00 * (double) num18 - m20 * (double) num21 + m30 * (double) num22) * num11;
            matrix44.m32 = (float) -(m00 * (double) num19 - m10 * (double) num21 + m30 * (double) num23) * num11;
            matrix44.m33 = (float) (m00 * (double) num20 - m10 * (double) num22 + m20 * (double) num23) * num11;
            return matrix44;
        }

        public static void Invert(ref Matrix4x4 matrix, out Matrix4x4 result)
        {
            var m00 = matrix.m00;
            var m10 = matrix.m10;
            var m20 = matrix.m20;
            var m30 = matrix.m30;
            var m01 = matrix.m01;
            var m11 = matrix.m11;
            var m21 = matrix.m21;
            var m31 = matrix.m31;
            var m02 = matrix.m02;
            var m12 = matrix.m12;
            var m22 = matrix.m22;
            var m32 = matrix.m32;
            var m03 = matrix.m03;
            var m13 = matrix.m13;
            var m23 = matrix.m23;
            var m33 = matrix.m33;
            var num1 = (float) (m22 * (double) m33 - m32 * (double) m23);
            var num2 = (float) (m12 * (double) m33 - m32 * (double) m13);
            var num3 = (float) (m12 * (double) m23 - m22 * (double) m13);
            var num4 = (float) (m02 * (double) m33 - m32 * (double) m03);
            var num5 = (float) (m02 * (double) m23 - m22 * (double) m03);
            var num6 = (float) (m02 * (double) m13 - m12 * (double) m03);
            var num7 = (float) (m11 * (double) num1 - m21 * (double) num2 + m31 * (double) num3);
            var num8 = (float) -(m01 * (double) num1 - m21 * (double) num4 + m31 * (double) num5);
            var num9 = (float) (m01 * (double) num2 - m11 * (double) num4 + m31 * (double) num6);
            var num10 = (float) -(m01 * (double) num3 - m11 * (double) num5 + m21 * (double) num6);
            var num11 = (float) (1.0 / (m00 * (double) num7 + m10 * (double) num8 + m20 * (double) num9 +
                                        m30 * (double) num10));
            result.m00 = num7 * num11;
            result.m01 = num8 * num11;
            result.m02 = num9 * num11;
            result.m03 = num10 * num11;
            result.m10 = (float) -(m10 * (double) num1 - m20 * (double) num2 + m30 * (double) num3) * num11;
            result.m11 = (float) (m00 * (double) num1 - m20 * (double) num4 + m30 * (double) num5) * num11;
            result.m12 = (float) -(m00 * (double) num2 - m10 * (double) num4 + m30 * (double) num6) * num11;
            result.m13 = (float) (m00 * (double) num3 - m10 * (double) num5 + m20 * (double) num6) * num11;
            var num12 = (float) (m21 * (double) m33 - m31 * (double) m23);
            var num13 = (float) (m11 * (double) m33 - m31 * (double) m13);
            var num14 = (float) (m11 * (double) m23 - m21 * (double) m13);
            var num15 = (float) (m01 * (double) m33 - m31 * (double) m03);
            var num16 = (float) (m01 * (double) m23 - m21 * (double) m03);
            var num17 = (float) (m01 * (double) m13 - m11 * (double) m03);
            result.m20 = (float) (m10 * (double) num12 - m20 * (double) num13 + m30 * (double) num14) * num11;
            result.m21 = (float) -(m00 * (double) num12 - m20 * (double) num15 + m30 * (double) num16) * num11;
            result.m22 = (float) (m00 * (double) num13 - m10 * (double) num15 + m30 * (double) num17) * num11;
            result.m23 = (float) -(m00 * (double) num14 - m10 * (double) num16 + m20 * (double) num17) * num11;
            var num18 = (float) (m21 * (double) m32 - m31 * (double) m22);
            var num19 = (float) (m11 * (double) m32 - m31 * (double) m12);
            var num20 = (float) (m11 * (double) m22 - m21 * (double) m12);
            var num21 = (float) (m01 * (double) m32 - m31 * (double) m02);
            var num22 = (float) (m01 * (double) m22 - m21 * (double) m02);
            var num23 = (float) (m01 * (double) m12 - m11 * (double) m02);
            result.m30 = (float) -(m10 * (double) num18 - m20 * (double) num19 + m30 * (double) num20) * num11;
            result.m31 = (float) (m00 * (double) num18 - m20 * (double) num21 + m30 * (double) num22) * num11;
            result.m32 = (float) -(m00 * (double) num19 - m10 * (double) num21 + m30 * (double) num23) * num11;
            result.m33 = (float) (m00 * (double) num20 - m10 * (double) num22 + m20 * (double) num23) * num11;
        }

        public static Matrix4x4 Add(Matrix4x4 matrix1, Matrix4x4 matrix2)
        {
            Matrix4x4 matrix44;
            matrix44.m00 = matrix1.m00 + matrix2.m00;
            matrix44.m01 = matrix1.m01 + matrix2.m01;
            matrix44.m02 = matrix1.m02 + matrix2.m02;
            matrix44.m03 = matrix1.m03 + matrix2.m03;
            matrix44.m10 = matrix1.m10 + matrix2.m10;
            matrix44.m11 = matrix1.m11 + matrix2.m11;
            matrix44.m12 = matrix1.m12 + matrix2.m12;
            matrix44.m13 = matrix1.m13 + matrix2.m13;
            matrix44.m20 = matrix1.m20 + matrix2.m20;
            matrix44.m21 = matrix1.m21 + matrix2.m21;
            matrix44.m22 = matrix1.m22 + matrix2.m22;
            matrix44.m23 = matrix1.m23 + matrix2.m23;
            matrix44.m30 = matrix1.m30 + matrix2.m30;
            matrix44.m31 = matrix1.m31 + matrix2.m31;
            matrix44.m32 = matrix1.m32 + matrix2.m32;
            matrix44.m33 = matrix1.m33 + matrix2.m33;
            return matrix44;
        }

        public static void Add(ref Matrix4x4 matrix1, ref Matrix4x4 matrix2, out Matrix4x4 result)
        {
            result.m00 = matrix1.m00 + matrix2.m00;
            result.m01 = matrix1.m01 + matrix2.m01;
            result.m02 = matrix1.m02 + matrix2.m02;
            result.m03 = matrix1.m03 + matrix2.m03;
            result.m10 = matrix1.m10 + matrix2.m10;
            result.m11 = matrix1.m11 + matrix2.m11;
            result.m12 = matrix1.m12 + matrix2.m12;
            result.m13 = matrix1.m13 + matrix2.m13;
            result.m20 = matrix1.m20 + matrix2.m20;
            result.m21 = matrix1.m21 + matrix2.m21;
            result.m22 = matrix1.m22 + matrix2.m22;
            result.m23 = matrix1.m23 + matrix2.m23;
            result.m30 = matrix1.m30 + matrix2.m30;
            result.m31 = matrix1.m31 + matrix2.m31;
            result.m32 = matrix1.m32 + matrix2.m32;
            result.m33 = matrix1.m33 + matrix2.m33;
        }

        public static Matrix4x4 Sub(Matrix4x4 matrix1, Matrix4x4 matrix2)
        {
            Matrix4x4 matrix44;
            matrix44.m00 = matrix1.m00 - matrix2.m00;
            matrix44.m01 = matrix1.m01 - matrix2.m01;
            matrix44.m02 = matrix1.m02 - matrix2.m02;
            matrix44.m03 = matrix1.m03 - matrix2.m03;
            matrix44.m10 = matrix1.m10 - matrix2.m10;
            matrix44.m11 = matrix1.m11 - matrix2.m11;
            matrix44.m12 = matrix1.m12 - matrix2.m12;
            matrix44.m13 = matrix1.m13 - matrix2.m13;
            matrix44.m20 = matrix1.m20 - matrix2.m20;
            matrix44.m21 = matrix1.m21 - matrix2.m21;
            matrix44.m22 = matrix1.m22 - matrix2.m22;
            matrix44.m23 = matrix1.m23 - matrix2.m23;
            matrix44.m30 = matrix1.m30 - matrix2.m30;
            matrix44.m31 = matrix1.m31 - matrix2.m31;
            matrix44.m32 = matrix1.m32 - matrix2.m32;
            matrix44.m33 = matrix1.m33 - matrix2.m33;
            return matrix44;
        }

        public static void Sub(ref Matrix4x4 matrix1, ref Matrix4x4 matrix2, out Matrix4x4 result)
        {
            result.m00 = matrix1.m00 - matrix2.m00;
            result.m01 = matrix1.m01 - matrix2.m01;
            result.m02 = matrix1.m02 - matrix2.m02;
            result.m03 = matrix1.m03 - matrix2.m03;
            result.m10 = matrix1.m10 - matrix2.m10;
            result.m11 = matrix1.m11 - matrix2.m11;
            result.m12 = matrix1.m12 - matrix2.m12;
            result.m13 = matrix1.m13 - matrix2.m13;
            result.m20 = matrix1.m20 - matrix2.m20;
            result.m21 = matrix1.m21 - matrix2.m21;
            result.m22 = matrix1.m22 - matrix2.m22;
            result.m23 = matrix1.m23 - matrix2.m23;
            result.m30 = matrix1.m30 - matrix2.m30;
            result.m31 = matrix1.m31 - matrix2.m31;
            result.m32 = matrix1.m32 - matrix2.m32;
            result.m33 = matrix1.m33 - matrix2.m33;
        }

        public static Matrix4x4 Multiply(Matrix4x4 matrix1, Matrix4x4 matrix2)
        {
            Matrix4x4 matrix44;
            matrix44.m00 = (float) (matrix1.m00 * (double) matrix2.m00 + matrix1.m01 * (double) matrix2.m10 +
                                    matrix1.m02 * (double) matrix2.m20 + matrix1.m03 * (double) matrix2.m30);
            matrix44.m01 = (float) (matrix1.m00 * (double) matrix2.m01 + matrix1.m01 * (double) matrix2.m11 +
                                    matrix1.m02 * (double) matrix2.m21 + matrix1.m03 * (double) matrix2.m31);
            matrix44.m02 = (float) (matrix1.m00 * (double) matrix2.m02 + matrix1.m01 * (double) matrix2.m12 +
                                    matrix1.m02 * (double) matrix2.m22 + matrix1.m03 * (double) matrix2.m32);
            matrix44.m03 = (float) (matrix1.m00 * (double) matrix2.m03 + matrix1.m01 * (double) matrix2.m13 +
                                    matrix1.m02 * (double) matrix2.m23 + matrix1.m03 * (double) matrix2.m33);
            matrix44.m10 = (float) (matrix1.m10 * (double) matrix2.m00 + matrix1.m11 * (double) matrix2.m10 +
                                    matrix1.m12 * (double) matrix2.m20 + matrix1.m13 * (double) matrix2.m30);
            matrix44.m11 = (float) (matrix1.m10 * (double) matrix2.m01 + matrix1.m11 * (double) matrix2.m11 +
                                    matrix1.m12 * (double) matrix2.m21 + matrix1.m13 * (double) matrix2.m31);
            matrix44.m12 = (float) (matrix1.m10 * (double) matrix2.m02 + matrix1.m11 * (double) matrix2.m12 +
                                    matrix1.m12 * (double) matrix2.m22 + matrix1.m13 * (double) matrix2.m32);
            matrix44.m13 = (float) (matrix1.m10 * (double) matrix2.m03 + matrix1.m11 * (double) matrix2.m13 +
                                    matrix1.m12 * (double) matrix2.m23 + matrix1.m13 * (double) matrix2.m33);
            matrix44.m20 = (float) (matrix1.m20 * (double) matrix2.m00 + matrix1.m21 * (double) matrix2.m10 +
                                    matrix1.m22 * (double) matrix2.m20 + matrix1.m23 * (double) matrix2.m30);
            matrix44.m21 = (float) (matrix1.m20 * (double) matrix2.m01 + matrix1.m21 * (double) matrix2.m11 +
                                    matrix1.m22 * (double) matrix2.m21 + matrix1.m23 * (double) matrix2.m31);
            matrix44.m22 = (float) (matrix1.m20 * (double) matrix2.m02 + matrix1.m21 * (double) matrix2.m12 +
                                    matrix1.m22 * (double) matrix2.m22 + matrix1.m23 * (double) matrix2.m32);
            matrix44.m23 = (float) (matrix1.m20 * (double) matrix2.m03 + matrix1.m21 * (double) matrix2.m13 +
                                    matrix1.m22 * (double) matrix2.m23 + matrix1.m23 * (double) matrix2.m33);
            matrix44.m30 = (float) (matrix1.m30 * (double) matrix2.m00 + matrix1.m31 * (double) matrix2.m10 +
                                    matrix1.m32 * (double) matrix2.m20 + matrix1.m33 * (double) matrix2.m30);
            matrix44.m31 = (float) (matrix1.m30 * (double) matrix2.m01 + matrix1.m31 * (double) matrix2.m11 +
                                    matrix1.m32 * (double) matrix2.m21 + matrix1.m33 * (double) matrix2.m31);
            matrix44.m32 = (float) (matrix1.m30 * (double) matrix2.m02 + matrix1.m31 * (double) matrix2.m12 +
                                    matrix1.m32 * (double) matrix2.m22 + matrix1.m33 * (double) matrix2.m32);
            matrix44.m33 = (float) (matrix1.m30 * (double) matrix2.m03 + matrix1.m31 * (double) matrix2.m13 +
                                    matrix1.m32 * (double) matrix2.m23 + matrix1.m33 * (double) matrix2.m33);
            return matrix44;
        }

        public static void Multiply(ref Matrix4x4 matrix1, ref Matrix4x4 matrix2, out Matrix4x4 result)
        {
            var num1 = (float) (matrix1.m00 * (double) matrix2.m00 + matrix1.m01 * (double) matrix2.m10 +
                                matrix1.m02 * (double) matrix2.m20 + matrix1.m03 * (double) matrix2.m30);
            var num2 = (float) (matrix1.m00 * (double) matrix2.m01 + matrix1.m01 * (double) matrix2.m11 +
                                matrix1.m02 * (double) matrix2.m21 + matrix1.m03 * (double) matrix2.m31);
            var num3 = (float) (matrix1.m00 * (double) matrix2.m02 + matrix1.m01 * (double) matrix2.m12 +
                                matrix1.m02 * (double) matrix2.m22 + matrix1.m03 * (double) matrix2.m32);
            var num4 = (float) (matrix1.m00 * (double) matrix2.m03 + matrix1.m01 * (double) matrix2.m13 +
                                matrix1.m02 * (double) matrix2.m23 + matrix1.m03 * (double) matrix2.m33);
            var num5 = (float) (matrix1.m10 * (double) matrix2.m00 + matrix1.m11 * (double) matrix2.m10 +
                                matrix1.m12 * (double) matrix2.m20 + matrix1.m13 * (double) matrix2.m30);
            var num6 = (float) (matrix1.m10 * (double) matrix2.m01 + matrix1.m11 * (double) matrix2.m11 +
                                matrix1.m12 * (double) matrix2.m21 + matrix1.m13 * (double) matrix2.m31);
            var num7 = (float) (matrix1.m10 * (double) matrix2.m02 + matrix1.m11 * (double) matrix2.m12 +
                                matrix1.m12 * (double) matrix2.m22 + matrix1.m13 * (double) matrix2.m32);
            var num8 = (float) (matrix1.m10 * (double) matrix2.m03 + matrix1.m11 * (double) matrix2.m13 +
                                matrix1.m12 * (double) matrix2.m23 + matrix1.m13 * (double) matrix2.m33);
            var num9 = (float) (matrix1.m20 * (double) matrix2.m00 + matrix1.m21 * (double) matrix2.m10 +
                                matrix1.m22 * (double) matrix2.m20 + matrix1.m23 * (double) matrix2.m30);
            var num10 = (float) (matrix1.m20 * (double) matrix2.m01 + matrix1.m21 * (double) matrix2.m11 +
                                 matrix1.m22 * (double) matrix2.m21 + matrix1.m23 * (double) matrix2.m31);
            var num11 = (float) (matrix1.m20 * (double) matrix2.m02 + matrix1.m21 * (double) matrix2.m12 +
                                 matrix1.m22 * (double) matrix2.m22 + matrix1.m23 * (double) matrix2.m32);
            var num12 = (float) (matrix1.m20 * (double) matrix2.m03 + matrix1.m21 * (double) matrix2.m13 +
                                 matrix1.m22 * (double) matrix2.m23 + matrix1.m23 * (double) matrix2.m33);
            var num13 = (float) (matrix1.m30 * (double) matrix2.m00 + matrix1.m31 * (double) matrix2.m10 +
                                 matrix1.m32 * (double) matrix2.m20 + matrix1.m33 * (double) matrix2.m30);
            var num14 = (float) (matrix1.m30 * (double) matrix2.m01 + matrix1.m31 * (double) matrix2.m11 +
                                 matrix1.m32 * (double) matrix2.m21 + matrix1.m33 * (double) matrix2.m31);
            var num15 = (float) (matrix1.m30 * (double) matrix2.m02 + matrix1.m31 * (double) matrix2.m12 +
                                 matrix1.m32 * (double) matrix2.m22 + matrix1.m33 * (double) matrix2.m32);
            var num16 = (float) (matrix1.m30 * (double) matrix2.m03 + matrix1.m31 * (double) matrix2.m13 +
                                 matrix1.m32 * (double) matrix2.m23 + matrix1.m33 * (double) matrix2.m33);
            result.m00 = num1;
            result.m01 = num2;
            result.m02 = num3;
            result.m03 = num4;
            result.m10 = num5;
            result.m11 = num6;
            result.m12 = num7;
            result.m13 = num8;
            result.m20 = num9;
            result.m21 = num10;
            result.m22 = num11;
            result.m23 = num12;
            result.m30 = num13;
            result.m31 = num14;
            result.m32 = num15;
            result.m33 = num16;
        }

        public static Vector4 TransformVector4(Matrix4x4 matrix, Vector4 vector)
        {
            var num1 = (float) (vector.x * (double) matrix.m00 + vector.y * (double) matrix.m01 +
                                vector.z * (double) matrix.m02 + vector.w * (double) matrix.m03);
            var num2 = (float) (vector.x * (double) matrix.m10 + vector.y * (double) matrix.m11 +
                                vector.z * (double) matrix.m12 + vector.w * (double) matrix.m13);
            var num3 = (float) (vector.x * (double) matrix.m20 + vector.y * (double) matrix.m21 +
                                vector.z * (double) matrix.m22 + vector.w * (double) matrix.m23);
            var num4 = (float) (vector.x * (double) matrix.m30 + vector.y * (double) matrix.m31 +
                                vector.z * (double) matrix.m32 + vector.w * (double) matrix.m33);
            Vector4 vector4;
            vector4.x = num1;
            vector4.y = num2;
            vector4.z = num3;
            vector4.w = num4;
            return vector4;
        }

        public static void TransformVector4(ref Matrix4x4 matrix, ref Vector4 vector, out Vector4 result)
        {
            var num1 = (float) (vector.x * (double) matrix.m00 + vector.y * (double) matrix.m01 +
                                vector.z * (double) matrix.m02 + vector.w * (double) matrix.m03);
            var num2 = (float) (vector.x * (double) matrix.m10 + vector.y * (double) matrix.m11 +
                                vector.z * (double) matrix.m12 + vector.w * (double) matrix.m13);
            var num3 = (float) (vector.x * (double) matrix.m20 + vector.y * (double) matrix.m21 +
                                vector.z * (double) matrix.m22 + vector.w * (double) matrix.m23);
            var num4 = (float) (vector.x * (double) matrix.m30 + vector.y * (double) matrix.m31 +
                                vector.z * (double) matrix.m32 + vector.w * (double) matrix.m33);
            result.x = num1;
            result.y = num2;
            result.z = num3;
            result.w = num4;
        }

        public static Vector3 TransformPosition(Matrix4x4 matrix, Vector3 position)
        {
            var num1 = (float) (position.x * (double) matrix.m00 + position.y * (double) matrix.m01 +
                                position.z * (double) matrix.m02) + matrix.m03;
            var num2 = (float) (position.x * (double) matrix.m10 + position.y * (double) matrix.m11 +
                                position.z * (double) matrix.m12) + matrix.m13;
            var num3 = (float) (position.x * (double) matrix.m20 + position.y * (double) matrix.m21 +
                                position.z * (double) matrix.m22) + matrix.m23;
            Vector3 vector3;
            vector3.x = num1;
            vector3.y = num2;
            vector3.z = num3;
            return vector3;
        }

        public static void TransformPosition(ref Matrix4x4 matrix, ref Vector3 position, out Vector3 result)
        {
            var num1 = (float) (position.x * (double) matrix.m00 + position.y * (double) matrix.m01 +
                                position.z * (double) matrix.m02) + matrix.m03;
            var num2 = (float) (position.x * (double) matrix.m10 + position.y * (double) matrix.m11 +
                                position.z * (double) matrix.m12) + matrix.m13;
            var num3 = (float) (position.x * (double) matrix.m20 + position.y * (double) matrix.m21 +
                                position.z * (double) matrix.m22) + matrix.m23;
            result.x = num1;
            result.y = num2;
            result.z = num3;
        }

        public Vector3 MultiplyPoint3x4(Vector3 point)
        {
            return TransformPosition(this, point);
        }

        public Vector3 MultiplyVector(Vector3 vector)
        {
            return TransformDirection(this, vector);
        }

        public static Vector3 TransformDirection(Matrix4x4 matrix, Vector3 direction)
        {
            var num1 = (float) (direction.x * (double) matrix.m00 + direction.y * (double) matrix.m01 +
                                direction.z * (double) matrix.m02);
            var num2 = (float) (direction.x * (double) matrix.m10 + direction.y * (double) matrix.m11 +
                                direction.z * (double) matrix.m12);
            var num3 = (float) (direction.x * (double) matrix.m20 + direction.y * (double) matrix.m21 +
                                direction.z * (double) matrix.m22);
            Vector3 vector3;
            vector3.x = num1;
            vector3.y = num2;
            vector3.z = num3;
            return vector3;
        }

        public static void TransformDirection(ref Matrix4x4 matrix, ref Vector3 direction, out Vector3 result)
        {
            var num1 = (float) (direction.x * (double) matrix.m00 + direction.y * (double) matrix.m01 +
                                direction.z * (double) matrix.m02);
            var num2 = (float) (direction.x * (double) matrix.m10 + direction.y * (double) matrix.m11 +
                                direction.z * (double) matrix.m12);
            var num3 = (float) (direction.x * (double) matrix.m20 + direction.y * (double) matrix.m21 +
                                direction.z * (double) matrix.m22);
            result.x = num1;
            result.y = num2;
            result.z = num3;
        }

        public static Matrix4x4 operator -(Matrix4x4 matrix1)
        {
            Matrix4x4 matrix44;
            matrix44.m00 = -matrix1.m00;
            matrix44.m01 = -matrix1.m01;
            matrix44.m02 = -matrix1.m02;
            matrix44.m03 = -matrix1.m03;
            matrix44.m10 = -matrix1.m10;
            matrix44.m11 = -matrix1.m11;
            matrix44.m12 = -matrix1.m12;
            matrix44.m13 = -matrix1.m13;
            matrix44.m20 = -matrix1.m20;
            matrix44.m21 = -matrix1.m21;
            matrix44.m22 = -matrix1.m22;
            matrix44.m23 = -matrix1.m23;
            matrix44.m30 = -matrix1.m30;
            matrix44.m31 = -matrix1.m31;
            matrix44.m32 = -matrix1.m32;
            matrix44.m33 = -matrix1.m33;
            return matrix44;
        }

        public static bool operator ==(Matrix4x4 matrix1, Matrix4x4 matrix2)
        {
            if (matrix1.m00 == (double) matrix2.m00 && matrix1.m11 == (double) matrix2.m11 &&
                matrix1.m22 == (double) matrix2.m22 && matrix1.m33 == (double) matrix2.m33 &&
                matrix1.m01 == (double) matrix2.m01 && matrix1.m02 == (double) matrix2.m02 &&
                matrix1.m03 == (double) matrix2.m03 && matrix1.m10 == (double) matrix2.m10 &&
                matrix1.m12 == (double) matrix2.m12 && matrix1.m13 == (double) matrix2.m13 &&
                matrix1.m20 == (double) matrix2.m20 && matrix1.m21 == (double) matrix2.m21 &&
                matrix1.m23 == (double) matrix2.m23 && matrix1.m30 == (double) matrix2.m30 &&
                matrix1.m31 == (double) matrix2.m31)
            {
                return matrix1.m32 == (double) matrix2.m32;
            }

            return false;
        }

        public static bool operator !=(Matrix4x4 matrix1, Matrix4x4 matrix2)
        {
            if (matrix1.m00 == (double) matrix2.m00 && matrix1.m01 == (double) matrix2.m01 &&
                matrix1.m02 == (double) matrix2.m02 && matrix1.m03 == (double) matrix2.m03 &&
                matrix1.m10 == (double) matrix2.m10 && matrix1.m11 == (double) matrix2.m11 &&
                matrix1.m12 == (double) matrix2.m12 && matrix1.m13 == (double) matrix2.m13 &&
                matrix1.m20 == (double) matrix2.m20 && matrix1.m21 == (double) matrix2.m21 &&
                matrix1.m22 == (double) matrix2.m22 && matrix1.m23 == (double) matrix2.m23 &&
                matrix1.m30 == (double) matrix2.m30 && matrix1.m31 == (double) matrix2.m31 &&
                matrix1.m32 == (double) matrix2.m32)
            {
                return matrix1.m33 != (double) matrix2.m33;
            }

            return true;
        }

        public static Matrix4x4 operator +(Matrix4x4 matrix1, Matrix4x4 matrix2)
        {
            Matrix4x4 matrix44;
            matrix44.m00 = matrix1.m00 + matrix2.m00;
            matrix44.m01 = matrix1.m01 + matrix2.m01;
            matrix44.m02 = matrix1.m02 + matrix2.m02;
            matrix44.m03 = matrix1.m03 + matrix2.m03;
            matrix44.m10 = matrix1.m10 + matrix2.m10;
            matrix44.m11 = matrix1.m11 + matrix2.m11;
            matrix44.m12 = matrix1.m12 + matrix2.m12;
            matrix44.m13 = matrix1.m13 + matrix2.m13;
            matrix44.m20 = matrix1.m20 + matrix2.m20;
            matrix44.m21 = matrix1.m21 + matrix2.m21;
            matrix44.m22 = matrix1.m22 + matrix2.m22;
            matrix44.m23 = matrix1.m23 + matrix2.m23;
            matrix44.m30 = matrix1.m30 + matrix2.m30;
            matrix44.m31 = matrix1.m31 + matrix2.m31;
            matrix44.m32 = matrix1.m32 + matrix2.m32;
            matrix44.m33 = matrix1.m33 + matrix2.m33;
            return matrix44;
        }

        public static Matrix4x4 operator -(Matrix4x4 matrix1, Matrix4x4 matrix2)
        {
            Matrix4x4 matrix44;
            matrix44.m00 = matrix1.m00 - matrix2.m00;
            matrix44.m01 = matrix1.m01 - matrix2.m01;
            matrix44.m02 = matrix1.m02 - matrix2.m02;
            matrix44.m03 = matrix1.m03 - matrix2.m03;
            matrix44.m10 = matrix1.m10 - matrix2.m10;
            matrix44.m11 = matrix1.m11 - matrix2.m11;
            matrix44.m12 = matrix1.m12 - matrix2.m12;
            matrix44.m13 = matrix1.m13 - matrix2.m13;
            matrix44.m20 = matrix1.m20 - matrix2.m20;
            matrix44.m21 = matrix1.m21 - matrix2.m21;
            matrix44.m22 = matrix1.m22 - matrix2.m22;
            matrix44.m23 = matrix1.m23 - matrix2.m23;
            matrix44.m30 = matrix1.m30 - matrix2.m30;
            matrix44.m31 = matrix1.m31 - matrix2.m31;
            matrix44.m32 = matrix1.m32 - matrix2.m32;
            matrix44.m33 = matrix1.m33 - matrix2.m33;
            return matrix44;
        }

        public static Matrix4x4 operator *(Matrix4x4 matrix1, Matrix4x4 matrix2)
        {
            Matrix4x4 matrix44;
            matrix44.m00 = (float) (matrix1.m00 * (double) matrix2.m00 + matrix1.m01 * (double) matrix2.m10 +
                                    matrix1.m02 * (double) matrix2.m20 + matrix1.m03 * (double) matrix2.m30);
            matrix44.m01 = (float) (matrix1.m00 * (double) matrix2.m01 + matrix1.m01 * (double) matrix2.m11 +
                                    matrix1.m02 * (double) matrix2.m21 + matrix1.m03 * (double) matrix2.m31);
            matrix44.m02 = (float) (matrix1.m00 * (double) matrix2.m02 + matrix1.m01 * (double) matrix2.m12 +
                                    matrix1.m02 * (double) matrix2.m22 + matrix1.m03 * (double) matrix2.m32);
            matrix44.m03 = (float) (matrix1.m00 * (double) matrix2.m03 + matrix1.m01 * (double) matrix2.m13 +
                                    matrix1.m02 * (double) matrix2.m23 + matrix1.m03 * (double) matrix2.m33);
            matrix44.m10 = (float) (matrix1.m10 * (double) matrix2.m00 + matrix1.m11 * (double) matrix2.m10 +
                                    matrix1.m12 * (double) matrix2.m20 + matrix1.m13 * (double) matrix2.m30);
            matrix44.m11 = (float) (matrix1.m10 * (double) matrix2.m01 + matrix1.m11 * (double) matrix2.m11 +
                                    matrix1.m12 * (double) matrix2.m21 + matrix1.m13 * (double) matrix2.m31);
            matrix44.m12 = (float) (matrix1.m10 * (double) matrix2.m02 + matrix1.m11 * (double) matrix2.m12 +
                                    matrix1.m12 * (double) matrix2.m22 + matrix1.m13 * (double) matrix2.m32);
            matrix44.m13 = (float) (matrix1.m10 * (double) matrix2.m03 + matrix1.m11 * (double) matrix2.m13 +
                                    matrix1.m12 * (double) matrix2.m23 + matrix1.m13 * (double) matrix2.m33);
            matrix44.m20 = (float) (matrix1.m20 * (double) matrix2.m00 + matrix1.m21 * (double) matrix2.m10 +
                                    matrix1.m22 * (double) matrix2.m20 + matrix1.m23 * (double) matrix2.m30);
            matrix44.m21 = (float) (matrix1.m20 * (double) matrix2.m01 + matrix1.m21 * (double) matrix2.m11 +
                                    matrix1.m22 * (double) matrix2.m21 + matrix1.m23 * (double) matrix2.m31);
            matrix44.m22 = (float) (matrix1.m20 * (double) matrix2.m02 + matrix1.m21 * (double) matrix2.m12 +
                                    matrix1.m22 * (double) matrix2.m22 + matrix1.m23 * (double) matrix2.m32);
            matrix44.m23 = (float) (matrix1.m20 * (double) matrix2.m03 + matrix1.m21 * (double) matrix2.m13 +
                                    matrix1.m22 * (double) matrix2.m23 + matrix1.m23 * (double) matrix2.m33);
            matrix44.m30 = (float) (matrix1.m30 * (double) matrix2.m00 + matrix1.m31 * (double) matrix2.m10 +
                                    matrix1.m32 * (double) matrix2.m20 + matrix1.m33 * (double) matrix2.m30);
            matrix44.m31 = (float) (matrix1.m30 * (double) matrix2.m01 + matrix1.m31 * (double) matrix2.m11 +
                                    matrix1.m32 * (double) matrix2.m21 + matrix1.m33 * (double) matrix2.m31);
            matrix44.m32 = (float) (matrix1.m30 * (double) matrix2.m02 + matrix1.m31 * (double) matrix2.m12 +
                                    matrix1.m32 * (double) matrix2.m22 + matrix1.m33 * (double) matrix2.m32);
            matrix44.m33 = (float) (matrix1.m30 * (double) matrix2.m03 + matrix1.m31 * (double) matrix2.m13 +
                                    matrix1.m32 * (double) matrix2.m23 + matrix1.m33 * (double) matrix2.m33);
            return matrix44;
        }
    }
}