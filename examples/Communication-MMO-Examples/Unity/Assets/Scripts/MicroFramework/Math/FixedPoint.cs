using System;
public struct FixedPoint
{
    private const int Scale = 10000; // 定义小数点的位置
    private readonly long rawValue; // 存储整数值

    private FixedPoint(long value)
    {
        rawValue = value;
    }

    public static FixedPoint Zero => new FixedPoint(0);
    public static FixedPoint One => new FixedPoint(Scale);
    public static FixedPoint PI => new FixedPoint(31416); // 这里假设 PI 乘以 10000

    public static FixedPoint Sin(FixedPoint angle)
    {
        return new FixedPoint((long)(Math.Sin(angle.ToFloat()) * Scale));
    }

    public static FixedPoint Cos(FixedPoint angle)
    {
        return new FixedPoint((long)(Math.Cos(angle.ToFloat()) * Scale));
    }

    public static FixedPoint FromInt(int value)
    {
        return new FixedPoint(value * Scale);
    }

    public static FixedPoint FromFloat(float value)
    {
        return new FixedPoint((long)(value * Scale));
    }

    public static FixedPoint operator +(FixedPoint a, FixedPoint b)
    {
        return new FixedPoint(a.rawValue + b.rawValue);
    }

    public static FixedPoint operator -(FixedPoint a, FixedPoint b)
    {
        return new FixedPoint(a.rawValue - b.rawValue);
    }

    public static FixedPoint operator *(FixedPoint a, FixedPoint b)
    {
        return new FixedPoint((a.rawValue * b.rawValue) / Scale);
    }

    public static FixedPoint operator /(FixedPoint a, FixedPoint b)
    {
        if (b.rawValue == 0)
        {
            throw new DivideByZeroException("Cannot divide by zero.");
        }
        return new FixedPoint((a.rawValue * Scale) / b.rawValue);
    }

    public static FixedPoint operator /(FixedPoint a, int divisor)
    {
        if (divisor == 0)
        {
            throw new DivideByZeroException("Cannot divide by zero.");
        }
        return new FixedPoint(a.rawValue / divisor);
    }

    public float ToFloat()
    {
        return (float)rawValue / Scale;
    }

    public override string ToString()
    {
        return ToFloat().ToString();
    }

    public static bool operator ==(FixedPoint a, FixedPoint b)
    {
        return a.rawValue == b.rawValue;
    }

    public static bool operator !=(FixedPoint a, FixedPoint b)
    {
        return a.rawValue != b.rawValue;
    }

    public override bool Equals(object obj)
    {
        if (obj is FixedPoint)
        {
            return this == (FixedPoint)obj;
        }
        return false;
    }

    public override int GetHashCode()
    {
        return rawValue.GetHashCode();
    }
}

// class Program
// {
//     static void Main()
//     {
//         FixedPoint fixed1 = FixedPoint.FromInt(5);
//         FixedPoint fixed2 = FixedPoint.FromInt(3);

//         FixedPoint resultAdd = fixed1 + fixed2;
//         Console.WriteLine(resultAdd); // 输出 0.8000

//         FixedPoint resultSubtract = fixed1 - fixed2;
//         Console.WriteLine(resultSubtract); // 输出 0.2000

//         FixedPoint resultMultiply = fixed1 * fixed2;
//         Console.WriteLine(resultMultiply); // 输出 1.6666

//         FixedPoint resultDivide = fixed1 / fixed2;
//         Console.WriteLine(resultDivide); // 输出 1.6666
//     }
// }
