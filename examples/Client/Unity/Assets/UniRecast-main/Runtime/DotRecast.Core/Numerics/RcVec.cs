using System;
using System.Runtime.CompilerServices;

namespace DotRecast.Core.Numerics
{
    public static class RcVec
    {
        public const float EPSILON = 1e-6f;
        public static readonly float EQUAL_THRESHOLD = RcMath.Sqr(1.0f / 16384.0f);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static RcVec3f ToVec3(this float[] values)
        {
            return new RcVec3f(values[0], values[1], values[2]);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static RcVec3f ToVec3(this float[] values, int n)
        {
            return new RcVec3f(values[n + 0], values[n + 1], values[n + 2]);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static RcVec3f ToVec3(this Span<float> values)
        {
            return new RcVec3f(values[0], values[1], values[2]);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static RcVec3f ToVec3(this Span<float> values, int n)
        {
            return new RcVec3f(values[n + 0], values[n + 1], values[n + 2]);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static RcVec3f ToVec3(this ReadOnlySpan<float> values, int n)
        {
            return new RcVec3f(values[n + 0], values[n + 1], values[n + 2]);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Get(this RcVec2f v, int i)
        {
            switch (i)
            {
                case 0: return v.X;
                case 1: return v.Y;
                default: throw new IndexOutOfRangeException("vector2f index out of range");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Get(this RcVec3f v, int i)
        {
            switch (i)
            {
                case 0: return v.X;
                case 1: return v.Y;
                case 2: return v.Z;
                default: throw new IndexOutOfRangeException("vector3f index out of range");
            }
        }

        /// Performs a 'sloppy' colocation check of the specified points.
        /// @param[in] p0 A point. [(x, y, z)]
        /// @param[in] p1 A point. [(x, y, z)]
        /// @return True if the points are considered to be at the same location.
        ///
        /// Basically, this function will return true if the specified points are
        /// close enough to eachother to be considered colocated.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Equal(RcVec3f p0, RcVec3f p1)
        {
            float d = RcVec3f.DistanceSquared(p0, p1);
            return d < EQUAL_THRESHOLD;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Dot2(RcVec3f a, RcVec3f b)
        {
            return a.X * b.X + a.Z * b.Z;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float DistSq2(float[] verts, int p, int q)
        {
            float dx = verts[q + 0] - verts[p + 0];
            float dy = verts[q + 2] - verts[p + 2];
            return dx * dx + dy * dy;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Dist2(float[] verts, int p, int q)
        {
            return MathF.Sqrt(DistSq2(verts, p, q));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float DistSq2(RcVec3f p, RcVec3f q)
        {
            float dx = q.X - p.X;
            float dy = q.Z - p.Z;
            return dx * dx + dy * dy;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Dist2(RcVec3f p, RcVec3f q)
        {
            return MathF.Sqrt(DistSq2(p, q));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Cross2(float[] verts, int p1, int p2, int p3)
        {
            float u1 = verts[p2 + 0] - verts[p1 + 0];
            float v1 = verts[p2 + 2] - verts[p1 + 2];
            float u2 = verts[p3 + 0] - verts[p1 + 0];
            float v2 = verts[p3 + 2] - verts[p1 + 2];
            return u1 * v2 - v1 * u2;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Cross2(RcVec3f p1, RcVec3f p2, RcVec3f p3)
        {
            float u1 = p2.X - p1.X;
            float v1 = p2.Z - p1.Z;
            float u2 = p3.X - p1.X;
            float v2 = p3.Z - p1.Z;
            return u1 * v2 - v1 * u2;
        }

        /// Derives the dot product of two vectors on the xz-plane. (@p u . @p v)
        /// @param[in] u A vector [(x, y, z)]
        /// @param[in] v A vector [(x, y, z)]
        /// @return The dot product on the xz-plane.
        ///
        /// The vectors are projected onto the xz-plane, so the y-values are
        /// ignored.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Dot2D(this RcVec3f @this, RcVec3f v)
        {
            return @this.X * v.X +
                   @this.Z * v.Z;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Cross(float[] dest, float[] v1, float[] v2)
        {
            dest[0] = v1[1] * v2[2] - v1[2] * v2[1];
            dest[1] = v1[2] * v2[0] - v1[0] * v2[2];
            dest[2] = v1[0] * v2[1] - v1[1] * v2[0];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Copy(float[] @out, int n, float[] @in, int m)
        {
            @out[n + 0] = @in[m + 0];
            @out[n + 1] = @in[m + 1];
            @out[n + 2] = @in[m + 2];
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Copy(Span<float> @out, int n, ReadOnlySpan<float> @in, int m)
        {
            @out[n + 0] = @in[m + 0];
            @out[n + 1] = @in[m + 1];
            @out[n + 2] = @in[m + 2];
        }

        /// Returns the distance between two points.
        /// @param[in] v1 A point. [(x, y, z)]
        /// @param[in] v2 A point. [(x, y, z)]
        /// @return The distance between the two points.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float DistanceSquared(RcVec3f v1, float[] v2, int i)
        {
            float dx = v2[i] - v1.X;
            float dy = v2[i + 1] - v1.Y;
            float dz = v2[i + 2] - v1.Z;
            return dx * dx + dy * dy + dz * dz;
        }

        /// Normalizes the vector if the length is greater than zero.
        /// If the magnitude is zero, the vector is unchanged.
        /// @param[in,out]	v	The vector to normalize. [(x, y, z)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static RcVec3f SafeNormalize(RcVec3f v)
        {
            float sqMag = RcMath.Sqr(v.X) + RcMath.Sqr(v.Y) + RcMath.Sqr(v.Z);
            if (sqMag > EPSILON)
            {
                float inverseMag = 1.0f / MathF.Sqrt(sqMag);
                return new RcVec3f(
                    v.X *= inverseMag,
                    v.Y *= inverseMag,
                    v.Z *= inverseMag
                );
            }

            return v;
        }

        /// Derives the distance between the specified points on the xz-plane.
        /// @param[in] v1 A point. [(x, y, z)]
        /// @param[in] v2 A point. [(x, y, z)]
        /// @return The distance between the point on the xz-plane.
        ///
        /// The vectors are projected onto the xz-plane, so the y-values are
        /// ignored.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Dist2D(RcVec3f v1, RcVec3f v2)
        {
            float dx = v2.X - v1.X;
            float dz = v2.Z - v1.Z;
            return (float)MathF.Sqrt(dx * dx + dz * dz);
        }

        /// Derives the square of the distance between the specified points on the xz-plane.
        ///  @param[in]		v1	A point. [(x, y, z)]
        ///  @param[in]		v2	A point. [(x, y, z)]
        /// @return The square of the distance between the point on the xz-plane.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Dist2DSqr(RcVec3f v1, RcVec3f v2)
        {
            float dx = v2.X - v1.X;
            float dz = v2.Z - v1.Z;
            return dx * dx + dz * dz;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Dist2DSqr(RcVec3f p, ReadOnlySpan<float> verts, int i)
        {
            float dx = verts[i] - p.X;
            float dz = verts[i + 2] - p.Z;
            return dx * dx + dz * dz;
        }

        /// Derives the xz-plane 2D perp product of the two vectors. (uz*vx - ux*vz)
        /// @param[in] u The LHV vector [(x, y, z)]
        /// @param[in] v The RHV vector [(x, y, z)]
        /// @return The dot product on the xz-plane.
        ///
        /// The vectors are projected onto the xz-plane, so the y-values are
        /// ignored.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Perp2D(RcVec3f u, RcVec3f v)
        {
            return u.Z * v.X - u.X * v.Z;
        }

        /// Checks that the specified vector's components are all finite.
        /// @param[in] v A point. [(x, y, z)]
        /// @return True if all of the point's components are finite, i.e. not NaN
        /// or any of the infinities.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsFinite(this RcVec3f v)
        {
            return float.IsFinite(v.X) && float.IsFinite(v.Y) && float.IsFinite(v.Z);
        }

        /// Checks that the specified vector's 2D components are finite.
        /// @param[in] v A point. [(x, y, z)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsFinite2D(this RcVec3f v)
        {
            return float.IsFinite(v.X) && float.IsFinite(v.Z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float PerpXZ(RcVec3f a, RcVec3f b)
        {
            return (a.X * b.Z) - (a.Z * b.X);
        }

        /// Performs a linear interpolation between two vectors. (@p v1 toward @p
        /// v2)
        /// @param[out] dest The result vector. [(x, y, x)]
        /// @param[in] v1 The starting vector.
        /// @param[in] v2 The destination vector.
        /// @param[in] t The interpolation factor. [Limits: 0 <= value <= 1.0]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static RcVec3f Lerp(ReadOnlySpan<float> verts, int v1, int v2, float t)
        {
            return new RcVec3f(
                verts[v1 + 0] + (verts[v2 + 0] - verts[v1 + 0]) * t,
                verts[v1 + 1] + (verts[v2 + 1] - verts[v1 + 1]) * t,
                verts[v1 + 2] + (verts[v2 + 2] - verts[v1 + 2]) * t
            );
        }

        /// Performs a scaled vector addition. (@p v1 + (@p v2 * @p s))
        /// @param[out] dest The result vector. [(x, y, z)]
        /// @param[in] v1 The base vector. [(x, y, z)]
        /// @param[in] v2 The vector to scale and add to @p v1. [(x, y, z)]
        /// @param[in] s The amount to scale @p v2 by before adding to @p v1.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static RcVec3f Mad(RcVec3f v1, RcVec3f v2, float s)
        {
            return new RcVec3f()
            {
                X = v1.X + (v2.X * s),
                Y = v1.Y + (v2.Y * s),
                Z = v1.Z + (v2.Z * s),
            };
        }
    }
}