namespace Fantasy.Core.Network
{
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