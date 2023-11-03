namespace Fantasy.Core.Network
{
    /// <summary>
    /// 表示网络通信的目标类型的枚举。
    /// </summary>
    public enum NetworkTarget
    {
        /// <summary>
        /// 未指定网络通信目标。
        /// </summary>
        None = 0,

        /// <summary>
        /// 表示外部网络通信目标。
        /// </summary>
        Outer = 1,

        /// <summary>
        /// 表示内部网络通信目标。
        /// </summary>
        Inner = 2
    }
}