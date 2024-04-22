namespace Fantasy
{
    /// <summary>
    /// 表示网络类型的枚举。
    /// </summary>
    public enum NetworkType
    {
        /// <summary>
        /// 未指定网络类型。
        /// </summary>
        None = 0,
        /// <summary>
        /// 表示客户端网络类型。
        /// </summary>
        Client = 1,
        /// <summary>
        /// 表示服务器网络类型。
        /// </summary>
        Server = 2
    }
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
    /// <summary>
    /// 表示网络通信协议类型的枚举。
    /// </summary>
    public enum NetworkProtocolType
    {
        /// <summary>
        /// 未指定协议类型。
        /// </summary>
        None = 0,
        /// <summary>
        /// 使用KCP（KCP协议）进行通信。
        /// </summary>
        KCP = 1,
        /// <summary>
        /// 使用TCP（传输控制协议）进行通信。
        /// </summary>
        TCP = 2
    }
}