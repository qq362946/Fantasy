using System;
using System.Runtime.CompilerServices;

namespace DotRecast.Core.Numerics
{
    public struct RcVec2f
    {
        public float X;
        public float Y;

        public static readonly RcVec2f Zero = new RcVec2f(0, 0);

        public RcVec2f(float x, float y)
        {
            X = x;
            Y = y;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is RcVec2f))
                return false;

            return Equals((RcVec2f)obj);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(RcVec2f other)
        {
            return X.Equals(other.X) &&
                   Y.Equals(other.Y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Distance(RcVec2f value1, RcVec2f value2)
        {
            float distanceSquared = RcVec2f.DistanceSquared(value1, value2);
            return MathF.Sqrt(distanceSquared);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float DistanceSquared(RcVec2f value1, RcVec2f value2)
        {
            RcVec2f difference = value1 - value2;
            return Dot(difference, difference);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Dot(RcVec2f value1, RcVec2f value2)
        {
            return (value1.X * value2.X)
                   + (value1.Y * value2.Y);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode()
        {
            int hash = X.GetHashCode();
            hash = RcHashCodes.CombineHashCodes(hash, Y.GetHashCode());
            return hash;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(RcVec2f left, RcVec2f right)
        {
            return left.Equals(right);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(RcVec2f left, RcVec2f right)
        {
            return !left.Equals(right);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static RcVec2f operator -(RcVec2f left, RcVec2f right)
        {
            return new RcVec2f(
                left.X - right.X,
                left.Y - right.Y
            );
        }

#if NET8_0_OR_GREATER
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator RcVec2f(System.Numerics.Vector2 v)
        {
            return Unsafe.BitCast<System.Numerics.Vector2, RcVec2f>(v);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator System.Numerics.Vector2(RcVec2f v)
        {
            return Unsafe.BitCast<RcVec2f, System.Numerics.Vector2>(v);
        }
#endif


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override string ToString()
        {
            return $"{X}, {Y}";
        }
    }
}