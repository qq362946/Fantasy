using System;
public struct FixedVector3
{
    public FixedPoint X { get; }
    public FixedPoint Y { get; }
    public FixedPoint Z { get; }

    public FixedVector3(FixedPoint x, FixedPoint y, FixedPoint z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    public static FixedVector3 operator +(FixedVector3 a, FixedVector3 b)
    {
        return new FixedVector3(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
    }

    public static FixedVector3 operator -(FixedVector3 a, FixedVector3 b)
    {
        return new FixedVector3(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
    }

    public static FixedVector3 operator *(FixedVector3 vector, FixedPoint scalar)
    {
        return new FixedVector3(vector.X * scalar, vector.Y * scalar, vector.Z * scalar);
    }

    public static FixedVector3 operator /(FixedVector3 vector, FixedPoint scalar)
    {
        if (scalar == FixedPoint.Zero)
        {
            throw new DivideByZeroException("Cannot divide by zero.");
        }
        return new FixedVector3(vector.X / scalar, vector.Y / scalar, vector.Z / scalar);
    }

    public static FixedVector3 operator /(FixedVector3 vector, float scalar)
    {
        if (scalar == 0)
        {
            throw new DivideByZeroException("Cannot divide by zero.");
        }
        FixedPoint fixedScalar = FixedPoint.FromFloat(scalar);
        return new FixedVector3(vector.X / fixedScalar, vector.Y / fixedScalar, vector.Z / fixedScalar);
    }

    public float Magnitude()
    {
        // 使用 Sqrt 时需要注意，如果 FixedPoint 是整数类型，可能需要转换成浮点数。
        return (float)Math.Sqrt(X.ToFloat() * X.ToFloat() + Y.ToFloat() * Y.ToFloat() + Z.ToFloat() * Z.ToFloat());
    }

    public static float SqrMagnitude(FixedVector3 vector)
    {
        FixedPoint sqrMag = vector.X * vector.X + vector.Y * vector.Y + vector.Z * vector.Z;
        return sqrMag.ToFloat();
    }

    public static float Distance(FixedVector3 p1,FixedVector3 p2){
        return (p1 - p2).Magnitude();
    }

    public FixedVector3 Normalize()
    {
        var magnitude = Magnitude();
        if (magnitude == 0)
        {
            return new FixedVector3(FixedPoint.Zero, FixedPoint.Zero, FixedPoint.Zero);
        }
        return this / magnitude;
    }

    public override string ToString()
    {
        return $"({X}, {Y}, {Z})";
    }
}

// class Program
// {
//     static void Main()
//     {
//         FixedVector3 point1 = new FixedVector3(FixedPoint.FromInt(3), FixedPoint.FromInt(4), FixedPoint.FromInt(5));
//         FixedVector3 point2 = new FixedVector3(FixedPoint.FromInt(1), FixedPoint.FromInt(2), FixedPoint.FromInt(3));

//         FixedVector3 resultAdd = point1 + point2;
//         Console.WriteLine(resultAdd); // 输出 (0.4000, 0.6000, 0.8000)

//         FixedVector3 resultSubtract = point1 - point2;
//         Console.WriteLine(resultSubtract); // 输出 (0.2000, 0.2000, 0.2000)

//         FixedVector3 resultScale = point1 * FixedPoint.FromInt(2);
//         Console.WriteLine(resultScale); // 输出 (0.6000, 0.8000, 1.0000)

//         FixedVector3 resultNormalize = point1.Normalize();
//         Console.WriteLine(resultNormalize); // 输出 (0.4243, 0.5657, 0.7071)
//     }
// }
