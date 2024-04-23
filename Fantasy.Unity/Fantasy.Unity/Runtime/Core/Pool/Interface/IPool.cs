namespace Fantasy
{
    /// <summary>
    /// 表示一个对象池接口。
    /// </summary>
    public interface IPool
    {
        /// <summary>
        /// 是否为对象池。
        /// </summary>
        bool IsPool { get; set; }
    }
}