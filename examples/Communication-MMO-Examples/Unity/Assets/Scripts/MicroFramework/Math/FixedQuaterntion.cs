public struct FixedQuaternion
{
    public FixedPoint X { get; }
    public FixedPoint Y { get; }
    public FixedPoint Z { get; }
    public FixedPoint W { get; }

    public FixedQuaternion(FixedPoint x, FixedPoint y, FixedPoint z, FixedPoint w)
    {
        X = x;
        Y = y;
        Z = z;
        W = w;
    }

    public static FixedQuaternion Identity => new FixedQuaternion(FixedPoint.Zero, FixedPoint.Zero, FixedPoint.Zero, FixedPoint.One);

    public static FixedQuaternion AngleAxis(FixedPoint angle, FixedVector3 axis)
    {
        // 转换为弧度
        FixedPoint radians = angle * (FixedPoint.PI / FixedPoint.FromInt(180));

        FixedPoint halfAngle = radians / FixedPoint.FromInt(2);
        FixedPoint sinHalf = FixedPoint.Sin(halfAngle);

        return new FixedQuaternion(axis.X * sinHalf, axis.Y * sinHalf, axis.Z * sinHalf, FixedPoint.Cos(halfAngle));
    }

    public static FixedQuaternion operator *(FixedQuaternion a, FixedQuaternion b)
    {
        return new FixedQuaternion(
            a.W * b.X + a.X * b.W + a.Y * b.Z - a.Z * b.Y,
            a.W * b.Y - a.X * b.Z + a.Y * b.W + a.Z * b.X,
            a.W * b.Z + a.X * b.Y - a.Y * b.X + a.Z * b.W,
            a.W * b.W - a.X * b.X - a.Y * b.Y - a.Z * b.Z
        );
    }

    // 可以添加其他四元数相关的操作和方法

    public override string ToString()
    {
        return $"({X}, {Y}, {Z}, {W})";
    }
}
