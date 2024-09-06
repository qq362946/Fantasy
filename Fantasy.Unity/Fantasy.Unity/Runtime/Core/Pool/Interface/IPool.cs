namespace Fantasy.Pool
{
    /// <summary>
    /// 实现了这个接口代表支持对象池
    /// </summary>
    public interface IPool
    {
        /// <summary>
        /// 是否从吃池里创建的
        /// </summary>
        bool IsPool { get; set; }
    }
}