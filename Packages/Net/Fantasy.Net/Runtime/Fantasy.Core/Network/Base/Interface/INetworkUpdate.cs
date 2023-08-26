namespace Fantasy.Core.Network
{
    /// <summary>
    /// 网络更新的接口。
    /// </summary>
    public interface INetworkUpdate
    {
        /// <summary>
        /// 在网络更新时调用的方法。
        /// </summary>
        void Update();
    }
}