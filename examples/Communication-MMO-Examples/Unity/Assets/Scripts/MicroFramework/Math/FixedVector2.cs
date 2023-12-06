using System;
public struct FixedVector2
{
    public FixedPoint X { get; }
    public FixedPoint Y { get; }

    public FixedVector2(FixedPoint x, FixedPoint y)
    {
        X = x;
        Y = y;
    }

    public static FixedVector2 operator +(FixedVector2 a, FixedVector2 b)
    {
        return new FixedVector2(a.X + b.X, a.Y + b.Y);
    }

    public static FixedVector2 operator -(FixedVector2 a, FixedVector2 b)
    {
        return new FixedVector2(a.X - b.X, a.Y - b.Y);
    }

    public static FixedVector2 operator *(FixedVector2 vector, FixedPoint scalar)
    {
        return new FixedVector2(vector.X * scalar, vector.Y * scalar);
    }

    public static FixedVector2 operator /(FixedVector2 vector, FixedPoint scalar)
    {
        if (scalar == FixedPoint.Zero)
        {
            throw new DivideByZeroException("Cannot divide by zero.");
        }
        return new FixedVector2(vector.X / scalar, vector.Y / scalar);
    }

    public static FixedVector2 operator /(FixedVector2 vector, float scalar)
    {
        if (scalar == 0)
        {
            throw new DivideByZeroException("Cannot divide by zero.");
        }
        FixedPoint fixedScalar = FixedPoint.FromFloat(scalar);
        return new FixedVector2(vector.X / fixedScalar, vector.Y / fixedScalar);
    }

    public float Magnitude()
    {
        // 使用 Sqrt 时需要注意，如果 FixedPoint 是整数类型，可能需要转换成浮点数。
        return (float)Math.Sqrt(X.ToFloat() * X.ToFloat() + Y.ToFloat() * Y.ToFloat());
    }

    public static float SqrMagnitude(FixedVector2 vector)
    {
        FixedPoint sqrMag = vector.X * vector.X + vector.Y * vector.Y;
        return sqrMag.ToFloat();
    }

    public static float Distance(FixedVector2 p1,FixedVector2 p2){
        return (p1 - p2).Magnitude();
    }

    public FixedVector2 Normalize()
    {
        var magnitude = Magnitude();
        if (magnitude == 0)
        {
            return new FixedVector2(FixedPoint.Zero, FixedPoint.Zero);
        }
        return this / magnitude;
    }

    public override string ToString()
    {
        return $"({X}, {Y})";
    }
}

// class Program
// {
//     static void Main()
//     {
//         FixedVector2 point1 = new FixedVector2(FixedPoint.FromInt(3), FixedPoint.FromInt(4));
//         FixedVector2 point2 = new FixedVector2(FixedPoint.FromInt(1), FixedPoint.FromInt(2));

//         FixedVector2 resultAdd = point1 + point2;
//         Console.WriteLine(resultAdd); // 输出 (0.4000, 0.6000)

//         FixedVector2 resultSubtract = point1 - point2;
//         Console.WriteLine(resultSubtract); // 输出 (0.2000, 0.2000)

//         FixedVector2 resultScale = point1 * FixedPoint.FromInt(2);
//         Console.WriteLine(resultScale); // 输出 (0.6000, 0.8000)

//         FixedVector2 resultNormalize = point1.Normalize();
//         Console.WriteLine(resultNormalize); // 输出 (0.6000, 0.8000)
//     }
// }
